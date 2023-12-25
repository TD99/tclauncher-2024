using System.IO;
using NAudio.Wave;

namespace TCLauncher.Core
{
    public class MusicPlayer
    {
        private IWavePlayer _waveOutDevice;
        private WaveStream _waveStream;

        public MusicPlayer(Stream musicStream)
        {
            _waveStream = new WaveFileReader(musicStream);
            _waveStream = new LoopStream(_waveStream);
            _waveOutDevice = new WaveOut();
            _waveOutDevice.Init(_waveStream);
        }

        public void Play()
        {
            _waveOutDevice.Play();
        }

        public void Stop()
        {
            _waveOutDevice.Stop();
        }

        public void SetVolume(float volume)
        {
            if (_waveOutDevice != null)
            {
                _waveOutDevice.Volume = volume;
            }
        }

        public void Dispose()
        {
            _waveOutDevice?.Dispose();
            _waveStream?.Dispose();
        }
    }
}
