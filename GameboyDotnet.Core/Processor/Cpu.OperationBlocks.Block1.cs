using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Processor;

public partial class Cpu
{
    /// <summary>
    /// HALT - Halt CPU until an interrupt occurs
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) Halt()
    {
        _logger.LogDebug("HALT - Halt CPU until an interrupt occurs");
        //TODO: Review if this is sufficient way to handle HALT
        if(Register.InterruptsEnabled)
            IsHalted = true;
        
        //TODO: Implement HALT bug
        return (1, 4);
    }


    /// <summary>
    /// LD r8, r8 - Load source register into destination register
    /// </summary>
    private (byte instructionBytesLength, byte durationTStates) LoadSourceR8IntoDestinationR8(ref byte opCode, byte destinationR8, byte sourceR8)
    {
        _logger.LogDebug("{opcode:X2} - LD {destinationR8:X2}, {sourceR8:X2}", opCode, destinationR8, sourceR8);
        if (destinationR8 == 0b110) //Destination: [HL]
        {
            MemoryController.WriteByte(Register.HL, Register.GetRegisterValueByR8(sourceR8));
            return (1, 8);
        }

        if (sourceR8 == 0b110) //Source: [HL]
        {
            Register.SetRegisterByR8(destinationR8, MemoryController.ReadByte(Register.HL));
            return (1, 8);
        }

        Register.SetRegisterByR8(destinationR8, Register.GetRegisterValueByR8(sourceR8));
        return (1, 4);
    }
}