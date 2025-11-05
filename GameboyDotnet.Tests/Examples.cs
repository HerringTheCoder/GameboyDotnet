using System.Text;
using GameboyDotnet.Components.Cpu;
using GameboyDotnet.Memory;

namespace GameboyDotnet.Tests;

public class Examples
{
    private readonly CpuRegister Register = new();
    private readonly MemoryController MemoryController;

    public byte Fetch()
    {
        var nextInstructionCode = MemoryController.ReadByte(Register.PC);
        return nextInstructionCode;
    }

    public void DecodeAndExecute(byte instructionCode)
    {
        switch (instructionCode)
        {
            case 0x3C: 
                IncA(); 
                break;
            default:
                throw new NotImplementedException();
        }
    }

    public void IncA()
    {
        //Increment
        Register.A++;

        //Set flags
        Register.NegativeFlag = false;
        Register.CarryFlag = Register.A == 0;
        Register.HalfCarryFlag = (Register.A & 0x0F) == 0;

        //Move to next instruction
        Register.PC++;
    }

    public void Nop()
    {
        
    }
}