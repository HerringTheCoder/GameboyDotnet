// ReSharper disable InconsistentNaming

namespace GameboyDotnet.Components.Cpu;

/// <summary>
/// Some of the registers are controlled as 16-bit, but sometimes we need to access them as 8-bit
/// i.e. AF register is 16-bit, but we might need to control A and F separately, thus the separate properties
/// Implementation based on: https://gbdev.io/pandocs/CPU_Registers_and_Flags.html
/// </summary>
public class CpuRegister
{
    private readonly byte[] R8LookupTable;

    public CpuRegister()
    {
        R8LookupTable = new byte[8]; //[B, C, D, E, H, L, NULL, A]
        AF = 0x01B0;
        BC = 0x0013;
        DE = 0x00D8;
        HL = 0x014D;
    }

    /// <summary>
    /// IME - Interrupt Master Enable flag
    /// </summary>
    public bool InterruptsMasterEnabled;

    public bool IMEPending;
    

    /// <summary>
    /// Stack pointer, always accessed as 16-bit
    /// </summary>
    public ushort SP = 0xFFFE;

    /// <summary>
    /// Program counter, always accessed as 16-bit
    /// </summary>
    public ushort PC = 0x0100;

    /// <summary>
    /// Contains accumulator and flags, splits into A and F
    /// </summary>
    public ushort AF
    {
        get => (ushort)(A << 8 | F);
        set
        {
            A = (byte)(value >> 8);
            F = (byte)(value & 0x00FF);
        }
    }

    /// <summary>
    /// High part of AF 16-bit register
    /// </summary>
    public byte A
    {
        get => R8LookupTable[7];
        set => R8LookupTable[7] = value;
    }

    /// <summary>
    /// Low part of AF 16-bit register
    /// </summary>
    public byte F { get; set; }

    /// <summary>
    /// Zero flag, aka 'z' flag
    /// </summary>
    public bool ZeroFlag
    {
        get => (F & 0b10000000) != 0;
        set => F = (byte)(value ? F | 0b10000000 : F & 0b01111111);
    }

    /// <summary>
    /// Subtract flag, used for DAA instruction
    /// </summary>
    public bool NegativeFlag
    {
        get => (F & 0b01000000) != 0;
        set => F = (byte)(value ? F | 0b01000000 : F & 0b10111111);
    }

    /// <summary>
    /// Half Carry flag, aka 'h' flag, used for DAA instruction
    /// </summary>
    public bool HalfCarryFlag
    {
        get => (F & 0b00100000) != 0;
        set => F = (byte)(value ? F | 0b00100000 : F & 0b11011111);
    }

    /// <summary>
    /// Carry flag, sometimes referred to as the "Cy" flag
    /// </summary>
    public bool CarryFlag
    {
        get => (F & 0b00010000) != 0;
        set => F = (byte)(value ? F | 0b00010000 : F & 0b11101111);
    }

    /// <summary>
    /// Splits into B and C registers
    /// </summary>
    public ushort BC
    {
        get => (ushort)(B << 8 | C);
        set
        {
            B = (byte)(value >> 8);
            C = (byte)(value & 0x00FF);
        }
    }

    /// <summary>
    /// High part of BC 16-bit register
    /// </summary>
    public byte B
    {
        get => R8LookupTable[0];
        set => R8LookupTable[0] = value;
    }

    /// <summary>
    /// Low part of BC 16-bit register
    /// </summary>
    public byte C
    {
        get => R8LookupTable[1];
        set => R8LookupTable[1] = value;
    }

    /// <summary>
    /// Splits into D and E registers
    /// </summary>
    public ushort DE
    {
        get => (ushort)(D << 8 | E);
        set
        {
            D = (byte)(value >> 8);
            E = (byte)(value & 0x00FF);
        }
    }

    /// <summary>
    /// High part of DE register
    /// </summary>
    public byte D
    {
        get => R8LookupTable[2];
        set => R8LookupTable[2] = value;
    }

    /// <summary>
    /// Low part of DE register
    /// </summary>
    public byte E
    {
        get => R8LookupTable[3];
        set => R8LookupTable[3] = value;
    }

    /// <summary>
    /// Splits into H and L registers
    /// </summary>
    public ushort HL
    {
        get => (ushort)(H << 8 | L);
        set
        {
            H = (byte)(value >> 8);
            L = (byte)(value & 0x00FF);
        }
    }

    /// <summary>
    /// Part of HL 16-bit register
    /// </summary>
    public byte H
    {
        get => R8LookupTable[4];
        set => R8LookupTable[4] = value;
    }

    /// <summary>
    /// Part of HL 16-bit register
    /// </summary>
    public byte L
    {
        get => R8LookupTable[5];
        set => R8LookupTable[5] = value;
    }

    public void SetRegisterByR16(int r16, ushort value)
    {
        switch (r16)
        {
            case 0:
                BC = value;
                break;
            case 1:
                DE = value;
                break;
            case 2:
                HL = value;
                break;
            case 3:
                SP = value;
                break;
        }
    }

    public ushort GetRegisterValueByR16(int r16)
    {
        switch (r16)
        {
            case 0:
                return BC;
            case 1:
                return DE;
            case 2:
                return HL;
            case 3:
                return SP;
            default:
                throw new ArgumentOutOfRangeException(nameof(r16), r16, "Invalid r16 value");
        }
    }

    public ushort GetRegisterValueByR16Mem(int r16mem)
    {
        ushort value = 0;
        switch (r16mem)
        {
            case 0:
                return BC;
            case 1:
                return DE;
            case 2:
                value = HL;
                HL++;
                return value;
            case 3:
                value = HL;
                HL--;
                return value;
            default:
                throw new ArgumentOutOfRangeException(nameof(r16mem), r16mem, "Invalid r16mem value");
        }
    }

    public byte GetRegisterValueByR8(byte r8)
    {
        if (r8 is > 7 or 6)
            throw new ArgumentOutOfRangeException(nameof(r8), r8, "Invalid r8 value");

        return R8LookupTable[r8];
    }

    public void SetRegisterByR8(byte r8, byte value)
    {
        if (r8 is > 7 or 6)
            throw new ArgumentOutOfRangeException(nameof(r8), r8, "Invalid r8 value");

        R8LookupTable[r8] = value;
    }
}