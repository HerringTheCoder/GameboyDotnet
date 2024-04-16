// ReSharper disable InconsistentNaming

namespace GameboyDotnet.Components.Cpu;

/// <summary>
/// Some of the registers are controlled as 16-bit, but sometimes we need to access them as 8-bit
/// i.e. AF register is 16-bit, but we might need to control A and F separately, thus the separate properties
/// Implementation based on: https://gbdev.io/pandocs/CPU_Registers_and_Flags.html
/// </summary>
public class CpuRegister
{
    /// <summary>
    /// IME - Interrupt Master Enable flag
    /// </summary>
    public bool InterruptsMasterEnabled { get; set; }
    public bool IMEPending { get; set; }

    private ushort _af { get; set; } = 0x01B0;

    private ushort _bc { get; set; } = 0x0013;

    private ushort _de { get; set; } = 0x00D8;

    private ushort _hl { get; set; } = 0x014D;

    /// <summary>
    /// Stack pointer, always accessed as 16-bit
    /// </summary>
    public ushort SP { get; set; } = 0xFFFE;

    /// <summary>
    /// Program counter, always accessed as 16-bit
    /// </summary>
    public ushort PC { get; set; } = 0x0100;

    /// <summary>
    /// Contains accumulator and flags, splits into A and F
    /// </summary>
    public ushort AF
    {
        get => _af;
        set => _af = (ushort)((value & 0xFFF0) | (_af & 0x00FF)); //TODO: Double check
    }

    /// <summary>
    /// High part of AF 16-bit register
    /// </summary>
    public byte A
    {
        get => (byte)(_af >> 8);
        set => _af = (ushort)((_af & 0x00FF) | (value << 8));
    }

    /// <summary>
    /// Low part of AF 16-bit register
    /// </summary>
    public byte F
    {
        get => (byte)(_af & 0x00FF);
        set => _af = (ushort)((_af & 0xFF00) | (value & 0xF0));
    }

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
        get => _bc;
        set => _bc = value;
    }

    /// <summary>
    /// High part of BC 16-bit register
    /// </summary>
    public byte B
    {
        get => (byte)(_bc >> 8);
        set => _bc = (ushort)((_bc & 0x00FF) | (value << 8));
    }

    /// <summary>
    /// Low part of BC 16-bit register
    /// </summary>
    public byte C
    {
        get => (byte)(_bc & 0x00FF);
        set => _bc = (ushort)((_bc & 0xFF00) | value);
    }

    /// <summary>
    /// Splits into D and E registers
    /// </summary>
    public ushort DE
    {
        get => _de;
        set => _de = value;
    }

    /// <summary>
    /// High part of DE register
    /// </summary>
    public byte D
    {
        get => (byte)(_de >> 8);
        set => _de = (ushort)((_de & 0x00FF) | (value << 8));
    }

    /// <summary>
    /// Low part of DE register
    /// </summary>
    public byte E
    {
        get => (byte)(_de & 0x00FF);
        set => _de = (ushort)((_de & 0xFF00) | value);
    }

    /// <summary>
    /// Splits into H and L registers
    /// </summary>
    public ushort HL
    {
        get => _hl;
        set => _hl = value;
    }

    /// <summary>
    /// Part of HL 16-bit register
    /// </summary>
    public byte H
    {
        get => (byte)(_hl >> 8);
        set => _hl = (ushort)((_hl & 0x00FF) | (value << 8));
    }

    /// <summary>
    /// Part of HL 16-bit register
    /// </summary>
    public byte L
    {
        get => (byte)(_hl & 0x00FF);
        set => _hl = (ushort)((_hl & 0xFF00) | value);
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
        return r8 switch
        {
            0 => B,
            1 => C,
            2 => D,
            3 => E,
            4 => H,
            5 => L,
            6 => throw new ArgumentOutOfRangeException(nameof(r8), "Cannot access memory directly with this method, use MemoryController instead"),
            7 => A,
            _ => throw new ArgumentOutOfRangeException(nameof(r8), r8, "Invalid r8 value")
        };
    }

    public void SetRegisterByR8(byte r8, byte value)
    {
        switch (r8)
        {
            case 0:
                B = value;
                break;
            case 1:
                C = value;
                break;
            case 2:
                D = value;
                break;
            case 3:
                E = value;
                break;
            case 4:
                H = value;
                break;
            case 5:
                L = value;
                break;
            case 6:
                throw new ArgumentOutOfRangeException(nameof(r8), "Cannot access memory directly with this method, use MemoryController instead");
            case 7:
                A = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(r8), r8, "Invalid r8 value");
        }
    }
}