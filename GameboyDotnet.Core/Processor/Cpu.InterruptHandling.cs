using System.Numerics;
using GameboyDotnet.Extensions;

namespace GameboyDotnet.Processor;

public partial class Cpu
{
    private static readonly ushort[] InterruptAddresses = { 0x0040, 0x0048, 0x0050, 0x0058, 0x0060 };
    
    private bool HandleInterrupt()
    {
        var interruptFlags = MemoryController.ReadByte(Constants.IFRegister);
        var interruptEnable = MemoryController.ReadByte(Constants.IERegister);
        var interrupt = (byte)(interruptFlags & interruptEnable);

        if (interrupt == 0 || !Register.InterruptsMasterEnabled) //TODO: https://gbdev.io/pandocs/halt.html
            return false;

        IsHalted = false;
        var interruptIndex = BitOperations.TrailingZeroCount(interrupt);
        Register.InterruptsMasterEnabled = false;
        MemoryController.WriteByte(Constants.IFRegister, interruptFlags.ClearBit(interruptIndex));
        PushStack(Register.PC);

        Register.PC = InterruptAddresses[interruptIndex];

        return true;
    }
}