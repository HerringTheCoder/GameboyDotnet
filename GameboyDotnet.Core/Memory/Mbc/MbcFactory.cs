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

        // ROM size: 32 KiB × (1 << romSizeByte), where each bank is 16 KiB
        // Therefore: numberOfRomBanks = 2 × (1 << romSizeByte)
        var numberOfRomBanks = 2 << romSizeByte;
        
        // RAM size parsing according to cartridge header specification
        var numberOfRamBanks = ramSize switch
        {
            0x00 => 0,  // No RAM
            0x02 => 1,  // 8 KiB (1 bank)
            0x03 => 4,  // 32 KiB (4 banks of 8 KiB each)
            0x04 => 16, // 128 KiB (16 banks of 8 KiB each)
            0x05 => 8,  // 64 KiB (8 banks of 8 KiB each)
            _ => 0      // Unknown/invalid, default to no RAM
        };

        return cartridgeType switch
        {
            0x00 => new Mbc0("MBC0 Rom only", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),
            0x01 => new Mbc1("MBC1", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),
            0x02 => new Mbc1("MBC1+Ram", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),
            0x03 => new Mbc1("MBC1+Ram+Battery", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),

            0x08 => new Mbc0("MBC0+Rom+Ram", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),
            0x09 => new Mbc0("MBC0+Rom+Ram+Battery", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),
            0x0D => new Mbc0("MBC0", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),

            0x0F => new Mbc3("MBC3+Timer+Battery", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),
            0x10 => new Mbc3("MBC3+Timer+Ram+Battery", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),
            0x11 => new Mbc3("MBC3", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),
            0x12 => new Mbc3("MBC3+Ram", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),
            0x13 => new Mbc3("MBC3+Ram+Battery", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),

            0x19 => new Mbc5("MBC5", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),
            0x1A => new Mbc5("MBC5+Ram", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),
            0x1B => new Mbc5("MBC5+Ram+Battery", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),
            0x1C => new Mbc5("MBC5+Rumble", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),
            0x1D => new Mbc5("MBC5+Rumble+Ram", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),
            0x1E => new Mbc5("MBC5+Rumble+Ram+Battery", bankSizeInBytes: 0x4000, numberOfRomBanks, numberOfRamBanks),

            _ => throw new Exception($"Unsupported cartridge type: 0x{cartridgeType:X2}")
        };
    }
}