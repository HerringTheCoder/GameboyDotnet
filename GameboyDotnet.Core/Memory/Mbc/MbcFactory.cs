using GameboyDotnet.Memory.BuildingBlocks;

namespace GameboyDotnet.Memory.Mbc;

public static class MbcFactory
{
    public static MemoryBankController CreateMbc(byte cartridgeType, byte romSizeByte, byte ramSize)
    {
        if (romSizeByte is 0x52 or 0x53 or 0x54)
        {
            throw new Exception("Unsupported ROM size");
        }

        var numberOfRomBanks = 0b10 << romSizeByte;

        return cartridgeType switch
        {
            0x00 => new Mbc0("MBC0 Rom only", bankSizeInBytes: 0x4000, numberOfRomBanks),
            0x01 => new Mbc1("MBC1", bankSizeInBytes: 0x4000, numberOfRomBanks),
            0x02 => new Mbc1("MBC1+Ram", bankSizeInBytes: 0x4000, numberOfRomBanks),
            0x03 => new Mbc1("MBC1+Ram+Battery", bankSizeInBytes: 0x4000, numberOfRomBanks),

            0x08 => new Mbc0("MBC0+Rom+Ram", bankSizeInBytes: 0x4000, numberOfRomBanks),
            0x09 => new Mbc0("MBC0+Rom+Ram+Battery", bankSizeInBytes: 0x4000, numberOfRomBanks),
            0x0D => new Mbc0("MBC0", bankSizeInBytes: 0x4000, numberOfRomBanks),

            0x0F => new Mbc3("MBC3+Timer+Battery", bankSizeInBytes: 0x4000, numberOfRomBanks),
            0x10 => new Mbc3("MBC3+Timer+Ram+Battery", bankSizeInBytes: 0x4000, numberOfRomBanks),
            0x11 => new Mbc3("MBC3", bankSizeInBytes: 0x4000, numberOfRomBanks),
            0x12 => new Mbc3("MBC3+Ram", bankSizeInBytes: 0x4000, numberOfRomBanks),
            0x13 => new Mbc3("MBC3+Ram+Battery", bankSizeInBytes: 0x4000, numberOfRomBanks),

            _ => throw new Exception("Unsupported cartridge type")
        };
    }
}