namespace GameboyDotnet;

public partial class Gameboy
{
    public event EventHandler ExceptionOccured = null!;
    protected virtual void OnExceptionOccured(EventArgs e)
    {
        ExceptionOccured.Invoke(this, e);
    }

    public event EventHandler DisplayUpdated = null!;
    protected virtual void OnDisplayUpdated(EventArgs e)
    {
        DisplayUpdated.Invoke(this, e);
    }
    
    public event EventHandler FrameLimiterSwitched = null!;
    protected virtual void OnFrameLimiterSwitched(EventArgs e)
    {
        FrameLimiterSwitched.Invoke(this, e);
    }
}