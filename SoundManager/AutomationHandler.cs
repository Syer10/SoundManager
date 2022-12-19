using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Automation;
using Microsoft.Win32;

namespace SoundManager
{
    public class AutomationHandler : IDisposable
    {
        private readonly List<ProcessHandler> _processes = new List<ProcessHandler>();

        public void Init()
        {
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
            Automation.AddAutomationFocusChangedEventHandler(OnFocusChangedHandler);
        }

        public void AddProcessHandler(ProcessHandler processHandler)
        {
            if (_processes.Any(otherProcessHandler => processHandler.Process.Id.Equals(otherProcessHandler.Process.Id)))
            {
                processHandler.Dispose();
            }
            else
            {
                Console.WriteLine("  Adding audio handler for {0}", processHandler.Process.ProcessName);
                processHandler.Process.Exited += delegate
                {
                    RemoveProcessHandler(processHandler);
                };
                _processes.Add(processHandler);
            }
        }

        private void OnFocusChangedHandler(object src, AutomationFocusChangedEventArgs args)
        {
            var element = src as AutomationElement;
            if (element == null) return;

            using (var process = Process.GetProcessById(element.Current.ProcessId))
            {
                foreach (var processHandler in _processes)
                {
                    if (processHandler.Process.ProcessName.Equals(process.ProcessName))
                    {
                        if (!processHandler.SimpleVolume.IsMuted) continue;
                        Console.WriteLine("  Unmuting: {0}", processHandler.Process.ProcessName);
                        processHandler.SimpleVolume.IsMuted = false;
                    }
                    else if (!processHandler.SimpleVolume.IsMuted)
                    {
                        Console.WriteLine("  Muting: {0}", processHandler.Process.ProcessName);
                        processHandler.SimpleVolume.IsMuted = true;
                    }
                    else if (processHandler.Process.HasExited)
                    {
                        RemoveProcessHandler(processHandler);
                    }
                }
            }
        }

        private void RemoveProcessHandler(ProcessHandler processHandler)
        {
            Console.WriteLine("  Removing audio handler for {0}", processHandler.Process.ProcessName);
            processHandler.Dispose();
            _processes.Remove(processHandler);
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

        public void Dispose()
        {
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            Automation.RemoveAutomationFocusChangedEventHandler(OnFocusChangedHandler);
        }
    }
}