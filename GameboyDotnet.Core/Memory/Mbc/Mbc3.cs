using GameboyDotnet.Memory.BuildingBlocks;

namespace GameboyDotnet.Memory.Mbc;

public class Mbc3( string name, int bankSizeInBytes, int numberOfBanks) 
    : MemoryBankController(name, bankSizeInBytes, numberOfBanks)
{
    public override void WriteByte(ref ushort address, ref byte value)
    {
        switch (address)
        {
            
        }
        base.WriteByte(ref address, ref value);
    }
}