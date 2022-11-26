using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace BWindower
{
    internal class WindowManager : IEnumerable<string>
    {
        private static readonly string SETTINGS_PATH = SETTINGS_PATH =
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\{Application.CompanyName}\{Application.ProductName
                }\settings.json";
        private readonly Dictionary<string, DateTime> AutoEnabled;

        private WindowManager(IEnumerable<string> processNames)
        {
            AutoEnabled = new Dictionary<string, DateTime>();

            foreach (var name in processNames)
                AutoEnabled[name] = DateTime.UtcNow;

            var unused = Task.Run(AutoWindowLoop);
        }

        internal static async Task<WindowManager> CreateAsync()
        {
            if (File.Exists(SETTINGS_PATH))
            {
                var json = await File.ReadAllTextAsync(SETTINGS_PATH);
                var processNames = JsonConvert.DeserializeObject<string[]>(json);
                return new WindowManager(processNames ?? Enumerable.Empty<string>());
            }

            var dir = Path.GetDirectoryName(SETTINGS_PATH) ?? throw new Exception($"Invalid path: '{SETTINGS_PATH}'");
            Directory.CreateDirectory(dir);
            File.Create(SETTINGS_PATH);
            return new WindowManager(Enumerable.Empty<string>());
        }

        private async Task SerializeAsync()
        {
            var json = JsonConvert.SerializeObject(AutoEnabled.Keys, Formatting.Indented);
            await File.WriteAllTextAsync(SETTINGS_PATH, json)
                .ConfigureAwait(false);
        }

        private async Task AutoWindowLoop()
        {
            await Task.Yield();

            while (true)
                try
                {
                    if (AutoEnabled.Count == 0)
                        await Task.Delay(2500);

                    foreach ((var key, var value) in AutoEnabled)
                    {
                        foreach (var proc in Process.GetProcessesByName(key))
                            if (proc.MainWindowHandle != IntPtr.Zero && !User32.IsBorderless(proc) && DateTime.UtcNow.Subtract(value)
                                    .TotalSeconds > 10)
                            {
                                User32.MakeBorderlessWindow(proc);
                                AutoEnabled[key] = DateTime.UtcNow;
                            }
                    }

                    await Task.Delay(2500);
                } catch
                {
                    //unused
                    //incase a process exits while we're getting it's info
                }

            // ReSharper disable once FunctionNeverReturns
        }

        internal bool Contains(string name) => AutoEnabled.ContainsKey(name);

        internal async Task Add(string name)
        {
            AutoEnabled.Add(name, DateTime.MinValue);
            await SerializeAsync()
                .ConfigureAwait(false);
        }

        internal async Task<bool> Remove(string name)
        {
            var result = AutoEnabled.Remove(name);

            await SerializeAsync()
                .ConfigureAwait(false);

            return result;
        }

        public IEnumerator<string> GetEnumerator() => AutoEnabled.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}