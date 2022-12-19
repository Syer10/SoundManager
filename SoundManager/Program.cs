using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CSCore.CoreAudioAPI;

namespace SoundManager
{
    internal static class Program
    {
        private static readonly AutomationHandler Handler = new AutomationHandler();

        private static List<string> _applications;
        private static List<AudioSessionManager2> _sessionManagers;
        
        public static void Main(string[] args)
        {
            string readContents;
            using (var streamReader = new StreamReader(args[0], Encoding.UTF8))
            {
                readContents = streamReader.ReadToEnd();
            }
            _applications = readContents.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries
            ).ToList();
            _sessionManagers = GetDefaultAudioSessionManager2(DataFlow.Render).ToList();
            foreach (var sessionManager in _sessionManagers)
            {
                sessionManager.SessionCreated += SystemEvents_SessionCreated;
            }
            
            foreach (var processHandler in GetProcesses(_applications, _sessionManagers))
            {
                Handler.AddProcessHandler(processHandler);
            }
            Handler.Init();
            

            Console.ReadKey();
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
        
        private static void SystemEvents_SessionCreated(object sender, SessionCreatedEventArgs e)
        {
            foreach (var processHandler in GetProcesses(_applications, _sessionManagers))
            {
                Handler.AddProcessHandler(processHandler);
            }
        }
    }
}