using GameboyDotnet.Memory.BuildingBlocks;

namespace GameboyDotnet.Memory.Mbc;

public class Mbc0(int startAddress, int endAddress, string name, int bankSizeInBytes, int numberOfBanks)
    : MemoryBankController(startAddress, endAddress, name, bankSizeInBytes, numberOfBanks)
{
    public override void WriteByte(ref ushort address, ref byte value)
    {
        if (address is >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd)
        {
            ExternalRam.WriteByte(ref address, ref value);
        }
    }

    public override byte ReadByte(ref ushort address)
    {
        return address is >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd
            ? ExternalRam.ReadByte(ref address)
            : MemorySpace[address - StartAddress];
    }
}