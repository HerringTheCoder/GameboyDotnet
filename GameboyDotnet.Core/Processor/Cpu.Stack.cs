namespace GameboyDotnet.Processor;

public partial class Cpu
{
    public ushort PopStack()
    {
        var value = MemoryController.ReadWord(Register.SP);
        Register.SP += 2;
        return value;
    }
    
    public void PushStack(ushort value)
    {
        Register.SP -= 2;
        MemoryController.WriteWord(Register.SP, value);
    }
}