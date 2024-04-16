using GameboyDotnet.Components;

namespace GameboyDotnet.PPU;

public class Ppu
{
    
    private int _tStatesCounter;

    public Lcd Lcd { get; }
    
    public Ppu(Lcd lcd)
    {
        Lcd = lcd;
    }

    public void CheckAndPush(byte tStates, MemoryController memoryController)
    {
        _tStatesCounter += tStates;
        if (_tStatesCounter >= Constants.DmgCyclesPerFrame) //TODO: GBC speed swap
        {
            var lcdc = memoryController.ReadByte(Constants.LYRegister);
            SetLcdStates(ref lcdc);
            
            _tStatesCounter -= Constants.DmgCyclesPerFrame;
        }
    }

    private void SetLcdStates(ref byte lcdc)
    {
        
    }
}