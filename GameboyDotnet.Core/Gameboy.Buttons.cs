using GameboyDotnet.Extensions;
using GameboyDotnet.Memory;

namespace GameboyDotnet;

public partial class Gameboy
{
    //0 = pressed, 1 = not pressed
    public IDictionary<string, byte> JoypadPressedStates = new Dictionary<string, byte>
    {
        { "A", 0b01_1110 },
        { "B", 0b01_1101 },
        { "Select", 0b01_1011 },
        { "Start", 0b01_0111 },
        { "Right", 0b10_1110 },
        { "Left", 0b10_1101 },
        { "Up", 0b10_1011 },
        { "Down", 0b10_0111 }
    };
    
    public void PressButton(string button)
    {
        var buttonState = JoypadPressedStates[button];
        
        if (!buttonState.IsBitSet(4))
        {
            Cpu.MemoryController.DpadStates = (byte)(Cpu.MemoryController.DpadStates & buttonState);
        }
        
        if(!buttonState.IsBitSet(5))
        {
            Cpu.MemoryController.ButtonStates = (byte)(Cpu.MemoryController.ButtonStates & buttonState);
        }
    }
    
    public void ReleaseButton(string keyReleased)
    {
        var buttonState = JoypadPressedStates[keyReleased];
        
        if (!buttonState.IsBitSet(4))
        {
            Cpu.MemoryController.DpadStates = (byte)(Cpu.MemoryController.DpadStates | ~buttonState);
        }
        
        if(!buttonState.IsBitSet(5))
        {
            Cpu.MemoryController.ButtonStates = (byte)(Cpu.MemoryController.ButtonStates | ~buttonState);
        }
    }
    
    public void UpdateJoypadState()
    {
        
        if((Cpu.MemoryController._joypadRegister & 0x0F) != 0x0F)
        {
            Cpu.MemoryController.WriteByte(
                address: Constants.IERegister,
                value: Cpu.MemoryController.ReadByte(Constants.IERegister).SetBit(4)
                );
        }
    }
}