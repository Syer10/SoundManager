using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using CSCore.CoreAudioAPI;
using Microsoft.Win32;

namespace SoundManager
{
    internal static class Program
    {
        private static readonly AutomationHandler Handler = new AutomationHandler();

        private static List<string> _applications;
        private static List<AudioSessionManager2> _sessionManagers;
        
        public static void Main(string[] args)
        {
            HideWindow();
            string readContents;
            using (var streamReader = new StreamReader(args[0], Encoding.UTF8))
            {
                readContents = streamReader.ReadToEnd();
            }
            _applications = readContents.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries
            ).ToList();

            SystemEvents.PowerModeChanged += SystemEvents_OnPowerChange;

            Init();

            Console.ReadKey();
        }

        private static void Init()
        {
            _sessionManagers = GetDefaultAudioSessionManager2(DataFlow.Render).ToList();
            foreach (var sessionManager in _sessionManagers)
            {
                sessionManager.SessionCreated += AudioSession_SessionCreated;
            }

            foreach (var processHandler in GetProcesses(_applications, _sessionManagers))
            {
                Handler.AddProcessHandler(processHandler);
            }
            Handler.Init();
        }

        private static void Dispose()
        {
            foreach (var sessionManager in _sessionManagers)
            {
                try
                {
                    sessionManager.SessionCreated -= AudioSession_SessionCreated;
                }
                catch (CoreAudioAPIException e)
                {
                    Console.WriteLine("Remove");
                    Console.WriteLine(e);
                }
                try
                {
                    sessionManager.Dispose();
                }
                catch (CoreAudioAPIException e)
                {
                    Console.WriteLine("Dispose");
                    Console.WriteLine(e);
                }
            }
            _sessionManagers = null;
            Handler.Dispose();
        }
        
        private static IEnumerable<AudioSessionManager2> GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumAudioEndpoints(dataFlow, DeviceState.Active);
            foreach (var device in devices)
            {
                // Console.WriteLine("Device: " + device.FriendlyName);
                var sessionManager = AudioSessionManager2.FromMMDevice(device);
                yield return sessionManager;
            }
        }

        private static List<ProcessHandler> GetProcesses(ICollection<string> names, List<AudioSessionManager2> sessionManagers)
        {
            var list = new List<ProcessHandler>();
            foreach (var sessionManager in sessionManagers)
            {
                using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                {
                    foreach (var session in sessionEnumerator)
                    {
                        var processHandler = new ProcessHandler(session);
                        if (names.Contains(processHandler.Process.ProcessName))
                        {
                            list.Add(processHandler);
                        }
                        else
                        {
                            processHandler.Dispose();
                        }
                    }
                }
            }

            return list;
        }
        
        private static void SystemEvents_OnPowerChange(object s, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                Console.WriteLine("Resume");
                try
                {
                    Dispose();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }

                try
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10));
                    Init();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }

            }
            else if (e.Mode == PowerModes.Suspend)
            {
                try
                {
                    Console.WriteLine("Suspend");
                    Dispose();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }

            }
            else if (e.Mode == PowerModes.StatusChange)
            {
                Console.WriteLine("StatusChange");
            }
        }

        private static void AudioSession_SessionCreated(object sender, SessionCreatedEventArgs e)
        {
            foreach (var processHandler in GetProcesses(_applications, _sessionManagers))
            {
                Handler.AddProcessHandler(processHandler);
            }
        }
        
        [DllImport("user32.dll")]
        private static extern int ShowWindow(int Handle, int showState);

        [DllImport("kernel32.dll")]
        public static extern int GetConsoleWindow();

        public static void HideWindow()
        {
            int win = GetConsoleWindow();
            ShowWindow(win, 0);
        }
    }
}