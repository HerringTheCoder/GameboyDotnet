using System.Collections.Concurrent;

namespace GameboyDotnet.Sound
{
    public class AudioBuffer
    {
        private readonly ConcurrentQueue<float[]> _sampleQueue = new();
        private readonly float[] _sampleBuffer = new float[BufferSize];
        private const int BufferSize = 1024;
        private int CurrentBufferIndex = 0;
        
        public void EnqueueSample(float sample)
        {
            _sampleBuffer[CurrentBufferIndex] = sample;
            CurrentBufferIndex++;

            if (CurrentBufferIndex >= BufferSize)
            {
                _sampleQueue.Enqueue(_sampleBuffer);
                CurrentBufferIndex = 0;
            }
        }

        public bool TryDequeueSamples(out float[]? samples)
        {
            return _sampleQueue.TryDequeue(out samples);
        }
    }
}
