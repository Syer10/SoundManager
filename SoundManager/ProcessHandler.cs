using System;
using System.Diagnostics;
using CSCore.CoreAudioAPI;

namespace SoundManager
{
    public class ProcessHandler : IDisposable
    {
        private readonly Process _process;
        private readonly SimpleAudioVolume _simpleVolume;
        private readonly AudioSessionControl2 _sessionControl;

        public ProcessHandler(Process process, SimpleAudioVolume simpleVolume, AudioSessionControl2 sessionControl)
        {
            _process = process;
            _simpleVolume = simpleVolume;
            _sessionControl = sessionControl;
        }
        
        public ProcessHandler(SimpleAudioVolume simpleVolume, AudioSessionControl2 sessionControl)
        {
            _process = Process.GetProcessById(sessionControl.ProcessID);
            _simpleVolume = simpleVolume;
            _sessionControl = sessionControl;
        }
        
        public ProcessHandler(AudioSessionControl session)
        {
            _simpleVolume = session.QueryInterface<SimpleAudioVolume>();
            _sessionControl = session.QueryInterface<AudioSessionControl2>();
            _process = Process.GetProcessById(_sessionControl.ProcessID);
        }

        public AudioSessionControl2 SessionControl => _sessionControl;

        public SimpleAudioVolume SimpleVolume => _simpleVolume;

        public Process Process => _process;

        public void Dispose()
        {
            _process.Dispose();
            _simpleVolume.Dispose();
            _sessionControl.Dispose();
        }
    }
}