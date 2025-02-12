using System.Collections.Concurrent;
using System.Diagnostics;

namespace GameboyDotnet.Graphics;

public class FrameBuffer
{
    private readonly ConcurrentQueue<byte[,]> _frameQueue = new();
    private int _frameCount = 0;
    private readonly Stopwatch _stopwatch = new();
    public double Fps = 0;
    
    public FrameBuffer()
    {
        _stopwatch.Start();
    }
    
    public void EnqueueFrame(Lcd lcd)
    {
        if (_frameQueue.Count < 10)  // Prevent excessive buffering, but keep latency low
            _frameQueue.Enqueue(lcd.Buffer);
        
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
}