using System.Collections.Concurrent;
using System.Diagnostics;

namespace GameboyDotnet.Graphics;

public class FrameBuffer
{
    private readonly ConcurrentQueue<byte[,]> _frameQueue = new();
    private readonly ConcurrentQueue<byte[,,]> _colorFrameQueue = new();
    private int _frameCount = 0;
    private readonly Stopwatch _stopwatch = new();
    public double Fps = 0;
    public bool IsCgbMode { get; set; }
    
    public FrameBuffer()
    {
        _stopwatch.Start();
    }
    
    public void EnqueueFrame(Lcd lcd)
    {
        if (_frameQueue.Count < 10)  // Prevent excessive buffering, but keep latency low
        {
            _frameQueue.Enqueue(lcd.Buffer);
            
            // Also enqueue color buffer for CGB mode
            if (IsCgbMode)
            {
                _colorFrameQueue.Enqueue(lcd.ColorBuffer);
            }
        }
        
        Interlocked.Increment(ref _frameCount);
        
        if (_stopwatch.ElapsedMilliseconds >= 1000)
        {
            Fps = Interlocked.Exchange(ref _frameCount, 0);
            _stopwatch.Restart();
        }
    }
    
    public bool TryDequeueFrame(out byte[,]? frame)
    {
        return _frameQueue.TryDequeue(out frame);
    }
    
    public bool TryDequeueColorFrame(out byte[,,]? colorFrame)
    {
        return _colorFrameQueue.TryDequeue(out colorFrame);
    }
}