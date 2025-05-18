using System.Collections.Concurrent;

namespace GameboyDotnet.Sound
{
    public class AudioBuffer
    {
        private readonly ConcurrentQueue<float[]> _sampleQueue = new();
        private readonly float[] _sampleBuffer = new float[BufferSize];
        private const int BufferSize = 1024 * 2;
        private int CurrentBufferIndex = 0;
        
        public void EnqueueSample(float leftSample, float rightSample)
        {
            _sampleBuffer[CurrentBufferIndex++] = leftSample;
            _sampleBuffer[CurrentBufferIndex++] = rightSample;
            
            if (CurrentBufferIndex >= BufferSize)
            {
                CurrentBufferIndex = 0;
                float[] block = new float[BufferSize];
                Array.Copy(_sampleBuffer, block, BufferSize);
             
                if(_sampleQueue.Count < 10)
                    _sampleQueue.Enqueue(block);
                
                CurrentBufferIndex = 0;
            }
        }

        public bool TryDequeueSamples(out float[]? samples)
        {
            return _sampleQueue.TryDequeue(out samples);
        }
    }
}
