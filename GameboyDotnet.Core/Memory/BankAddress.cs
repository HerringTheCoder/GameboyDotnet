namespace GameboyDotnet.Memory;

public static class BankAddress
{
    public const ushort RomBank0Start = 0x0000;
    public const ushort RomBank0End = 0x3FFF;
    public const ushort RomBankNnStart = 0x4000;
    public const ushort RomBankNnEnd = 0x7FFF;
    public const ushort VramStart = 0x8000;
    public const ushort VramEnd = 0x9FFF;
    public const ushort ExternalRamStart = 0xA000;
    public const ushort ExternalRamEnd = 0xBFFF;
    public const ushort Wram0Start = 0xC000;
    public const ushort Wram0End = 0xCFFF;
    public const ushort Wram1Start = 0xD000;
    public const ushort Wram1End = 0xDFFF;
    public const ushort EchoRamStart = 0xE000;
    public const ushort EchoRamEnd = 0xFDFF;
    public const ushort OamStart = 0xFE00;
    public const ushort OamEnd = 0xFE9F;
    public const ushort NotUsableStart = 0xFEA0;
    public const ushort NotUsableEnd = 0xFEFF;
    public const ushort IoRegistersStart = 0xFF00;
    public const ushort IoRegistersEnd = 0xFF7F;
    public const ushort HRamStart = 0xFF80;
    public const ushort HRamEnd = 0xFFFE;
    public const ushort InterruptEnableRegisterStart = 0xFFFF;
    public const ushort InterruptEnableRegisterEnd = 0xFFFF;
}