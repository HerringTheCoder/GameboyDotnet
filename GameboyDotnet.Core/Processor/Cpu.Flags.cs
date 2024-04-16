namespace GameboyDotnet.Processor;

public partial class Cpu
{
    private void Set16BitAddCarryFlags(ushort a, ushort b)
    {
        Register.NegativeFlag = false;
        Register.HalfCarryFlag = (a & 0xFFF) + (b & 0xFFF) > 0xFFF; //Check overflow from 11 bit to 12 bit
        Register.CarryFlag = a + b > 0xFFFF;
    }

    private void SetSPSignedByteAddFlags(ushort sp, byte signedByte)
    {
        Register.ZeroFlag = false;
        Register.NegativeFlag = false;
        Register.HalfCarryFlag = (sp & 0xF) + (signedByte & 0xF) > 0xF;
        Register.CarryFlag = (sp & 0xFF) + signedByte > 0xFF;
    }

    private void Set8BitAddCarryFlags(byte a, byte b, byte? addCarryFlag = null)
    {
        //If carry flag is not being added (null), then set value to 0 to not interfere with flags
        var adCarryFlag = addCarryFlag ?? 0;
        Register.NegativeFlag = false;
        Register.ZeroFlag = (byte)(a + b + adCarryFlag) == 0;
        Register.HalfCarryFlag = (a & 0xF) + (b & 0xF) + adCarryFlag > 0xF;
        Register.CarryFlag = a + b + adCarryFlag > 0xFF;
    }

    private void Set8BitIncrementCarryFlags(byte a)
    {
        Register.NegativeFlag = false;
        Register.ZeroFlag = (byte)(a + 1) == 0;
        Register.HalfCarryFlag = (a & 0xF) + 1 > 0xF; //Check overflow from 3 bit to 4 bit
    }

    private void Set8BitDecrementCarryFlags(byte value)
    {
        Register.NegativeFlag = true;
        Register.ZeroFlag = (byte)(value - 1) == 0;
        Register.HalfCarryFlag = (value & 0xF) == 0;
    }

    private void Set8BitSubtractCompareFlags(byte a, byte b, byte? subtractCarryFlag = null)
    {
        //If carry flag is not being subtracted (null), then set value to 0 to not interfere with flags
        var sbCarryFlag = subtractCarryFlag ?? 0;
        Register.NegativeFlag = true;
        Register.ZeroFlag = (byte)(a - b - sbCarryFlag) == 0;
        Register.HalfCarryFlag = (a & 0xF) < (b & 0xF) + sbCarryFlag;
        Register.CarryFlag = a < b + sbCarryFlag;
    }

    private void Set8BitAndFlags()
    {
        Register.ZeroFlag = Register.A == 0;
        Register.HalfCarryFlag = true;
        (Register.NegativeFlag, Register.CarryFlag) = (false, false);
    }

    private void Set8BitOrXorFlags()
    {
        Register.ZeroFlag = Register.A == 0;
        (Register.NegativeFlag, Register.HalfCarryFlag, Register.CarryFlag) = (false, false, false);
    }

    private void SetPopFlags(ref ushort poppedValue)
    {
        var lowByte = (byte)(poppedValue & 0x00FF);
        Register.ZeroFlag = (lowByte & 0b10000000) != 0;
        Register.NegativeFlag = (lowByte & 0b01000000) != 0;
        Register.HalfCarryFlag = (lowByte & 0b00100000) != 0;
        Register.CarryFlag = (lowByte & 0b00010000) != 0;
    }
}