using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BWindower
{
    internal class User32
    {
        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) =>
            IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLongPtr32(hWnd, nIndex);

        internal static void MakeBorderlessWindow(int procId)
        {
            //make it borderless windowed
            var proc = Process.GetProcessById(procId);
            MakeBorderlessWindow(proc);
        }

        internal static void MakeBorderlessWindow(Process process)
        {
            SetWindowLong(process.MainWindowHandle, -16, 0x10000000);
            ShowWindowAsync(process.MainWindowHandle, 3);
        }

        internal static bool IsBorderless(Process process)
        {
            var wLong = GetWindowLongPtr(process.MainWindowHandle, -16)
                .ToInt64();

            //window is maximized and has no title bar
            return (wLong & 0x1000000) != 0 && (wLong & 0xC00000) == 0;
        }
    }
}