using System;
using System.Collections.Generic;
using CSCore.CoreAudioAPI;

namespace SoundManager
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            var list = GetProcesses(args);

            if (list.Count > 0)
            {
                var handler = new AutomationHandler(list);
                handler.Init();
            }

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

        private static List<ProcessHandler> GetProcesses(ICollection<string> names)
        {
            var list = new List<ProcessHandler>();
            foreach (var sessionManager in GetDefaultAudioSessionManager2(DataFlow.Render))
            {
                using (sessionManager)
                {
                    using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                    {
                        foreach (var session in sessionEnumerator)
                        {
                            var processHandler = new ProcessHandler(session);
                            // Console.WriteLine("Name: {0}, Identifier: {1}", processHandler.SessionControl.Process.ProcessName, processHandler.SessionControl.SessionIdentifier);

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
            }

            return list;
        }
    }
}