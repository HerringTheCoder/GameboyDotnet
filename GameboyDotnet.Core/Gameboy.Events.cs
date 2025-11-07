namespace GameboyDotnet;

public partial class Gameboy
{
    public event EventHandler ExceptionOccured = null!;
    protected virtual void OnExceptionOccured(EventArgs e)
    {
        ExceptionOccured.Invoke(this, e);
    }
}