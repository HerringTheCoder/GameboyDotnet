using GameboyDotnet.Memory.BuildingBlocks;

namespace GameboyDotnet.Memory.Mbc;

public class Mbc0(string name, int bankSizeInBytes, int numberOfBanks, int ramBankCount)
    : MemoryBankController(name, bankSizeInBytes, numberOfBanks, ramBankCount)
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

    public override void IncrementByte(ref ushort memoryAddress)
    {
        if (memoryAddress is >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd)
        {
            ExternalRam.IncrementByte(ref memoryAddress);
        }
    }

    public override void DecrementByte(ref ushort memoryAddress)
    {
        if (memoryAddress is >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd)
        {
            ExternalRam.DecrementByte(ref memoryAddress);
        }
    }
}