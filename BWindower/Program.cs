using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using BWindower.Properties;

namespace BWindower
{
    internal class Windower : Form
    {
        private const string PROCESS_NAME_REGEX = @"(.+) \[(\d+)\]|.*";
        private static bool AllowShowDisplay;
        private readonly WindowManager WindowManager;

        private static IEnumerable<Process> Processes =>
            Process.GetProcesses()
                .Where(proc => proc.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(proc.MainWindowTitle) && proc.Responding)
                .OrderBy(proc => proc.ProcessName);

        private Windower(WindowManager manager)
        {
            WindowManager = manager;
            var notifyIcon = new NotifyIcon
            {
                ContextMenuStrip = new ContextMenuStrip(),
                Icon = Resources.window_2,
                Text = @"BWindower",
                Visible = true
            };

            var windowIt = new ToolStripMenuItem("BWindow It!") { Name = "BWindow It!" };
            var autoWindowIt = new ToolStripMenuItem("Auto BWindow It!") { Name = "Auto BWindow It!" };
            var exit = new ToolStripMenuItem("Exit", null, Exit) { Name = "Exit" };

            notifyIcon.ContextMenuStrip.Items.Add(windowIt);
            notifyIcon.ContextMenuStrip.Items.Add(autoWindowIt);
            notifyIcon.ContextMenuStrip.Items.Add(exit);
            notifyIcon.ContextMenuStrip.Opening += Menu_Opening;
        }

        private static async Task Main()
        {
            var windowManager = await WindowManager.CreateAsync();
            Application.Run(new Windower(windowManager) { Visible = false });
        }

        private void Menu_Opening(object sender, EventArgs e)
        {
            //populate process list with any process that has a window associated with it
            //format as "processName [processId]"
            AllowShowDisplay = true;
            var contextMenu = (ContextMenuStrip) sender;
            var processes = Processes.ToList();

            var windowItProcs = processes.Select(proc =>
                    new ToolStripMenuItem($@"{proc.ProcessName.Replace(".exe", string.Empty)} [{proc.Id}]", null, WindowIt))
                .ToArray<ToolStripItem>();
            var autoWindowProcs = processes.Select(proc =>
                    new ToolStripMenuItem($@"{proc.ProcessName.Replace(".exe", string.Empty)} [{proc.Id}]", null, AutoWindowIt))
                .Concat(WindowManager.Except(processes.Select(proc => proc.ProcessName.Replace(".exe", string.Empty)))
                    .Select(enabledName => new ToolStripMenuItem(enabledName, null, AutoWindowIt)))
                .ToArray<ToolStripItem>();

            foreach (var menuItem in autoWindowProcs.OfType<ToolStripMenuItem>())
            {
                var match = Regex.Match(menuItem.Text, PROCESS_NAME_REGEX);
                var appName = match.Groups[1]
                    .Value;

                if (WindowManager.Contains(appName))
                    menuItem.Checked = true;
            }

            var windowIt = (ToolStripMenuItem) contextMenu.Items["BWindow It!"];
            windowIt.DropDownItems.Clear();
            windowIt.DropDownItems.AddRange(windowItProcs);

            var autoWindowIt = (ToolStripMenuItem) contextMenu.Items["Auto BWindow It!"];
            autoWindowIt.DropDownItems.Clear();
            autoWindowIt.DropDownItems.AddRange(autoWindowProcs);
        }

        private static void WindowIt(object sender, EventArgs e)
        {
            try
            {
                //get the processName and processId through regex
                var menuItem = (ToolStripMenuItem) sender;
                var match = Regex.Match(menuItem.Text, PROCESS_NAME_REGEX);
                var procId = int.Parse(match.Groups[2]
                    .Value);

                User32.MakeBorderlessWindow(procId);
            } catch
            {
                MessageBox.Show(@"Could not window, sorry!");
            }
        }

        private async void AutoWindowIt(object sender, EventArgs e)
        {
            try
            {
                var menuItem = (ToolStripMenuItem) sender;
                var match = Regex.Match(menuItem.Text, PROCESS_NAME_REGEX);
                var appName = match.Groups[1]
                    .Value;
                
                //if this appname and procid pair have already been added, theyre trying to disable it
                if (WindowManager.Contains(appName))
                {
                    await WindowManager.Remove(appName);
                    menuItem.Checked = false;
                    return;
                }

                var procId = int.Parse(match.Groups[2]
                    .Value);

                User32.MakeBorderlessWindow(procId);
                await WindowManager.Add(appName);
                menuItem.Checked = true;
            } catch
            {
                MessageBox.Show(@"Could not window, sorry!");
            }
        }

        protected override void SetVisibleCore(bool value) => base.SetVisibleCore(AllowShowDisplay ? value : AllowShowDisplay);

        private static void Exit(object sender, EventArgs e) => Application.Exit();
    }
}