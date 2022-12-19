using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Automation;
using Microsoft.Win32;

namespace SoundManager
{
    public class AutomationHandler
    {
        private readonly List<ProcessHandler> _processes;

        public AutomationHandler(List<ProcessHandler> processes)
        {
            _processes = processes;
        }

        public void Init()
        {
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            Automation.AddAutomationFocusChangedEventHandler(OnFocusChangedHandler);
        }

        private void OnFocusChangedHandler(object src, AutomationFocusChangedEventArgs args)
        {
            // Console.WriteLine("Focus changed!");
            var element = src as AutomationElement;
            if (element == null) return;

            var name = element.Current.Name;
            var id = element.Current.AutomationId;
            var processId = element.Current.ProcessId;

            using (var process = Process.GetProcessById(processId))
            {
                foreach (var processHandler in _processes)
                {
                    processHandler.SimpleVolume.IsMuted = !processHandler.Process.ProcessName.Equals(process.ProcessName);
                }
                Console.WriteLine("  Name: {0}, Id: {1}, Process: {2}", name, id, process.ProcessName);
            }
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                foreach (var processHandler in _processes)
                {
                    processHandler.SimpleVolume.IsMuted = true;
                }
                Automation.RemoveAutomationFocusChangedEventHandler(OnFocusChangedHandler);
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                Automation.RemoveAutomationFocusChangedEventHandler(OnFocusChangedHandler);
                Automation.AddAutomationFocusChangedEventHandler(OnFocusChangedHandler);
            }
        }
    }
}