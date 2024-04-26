namespace GameboyDotnet.Memory.Mbc;

public class Mbc0Mock(string name, int bankSizeInBytes, int numberOfBanks) 
    : Mbc0( name, bankSizeInBytes, numberOfBanks)
{
    public override void WriteByte(ref ushort address, ref byte value)
    {
        if (address is >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd)
        {
            ExternalRam.WriteByte(ref address, ref value);
            return;
        }
        
        base.WriteByte(ref address, ref value);
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
            return;
        }

        base.IncrementByte(ref memoryAddress);
    }
    
    public override void DecrementByte(ref ushort memoryAddress)
    {
        if (memoryAddress is >= BankAddress.ExternalRamStart and <= BankAddress.ExternalRamEnd)
        {
            ExternalRam.DecrementByte(ref memoryAddress);
            return;
        }

        base.DecrementByte(ref memoryAddress);
    }
}