namespace GameboyDotnet.Components;

public class Lcd
{
    public byte[,] Buffer = new byte[160,144];
    private readonly MemoryController _memoryController;
    public byte Lcdc => _memoryController.ReadByte(Constants.LCDControlRegister);
    public byte Lyc => _memoryController.ReadByte(Constants.LYCompareRegister);
    public byte Stat => _memoryController.ReadByte(Constants.LcdStatusRegister);
    

    public Lcd(MemoryController memoryController)
    {
        _memoryController = memoryController;
    }
    
}