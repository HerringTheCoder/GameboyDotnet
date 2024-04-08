namespace GameboyDotnet.Memory.BuildingBlocks;

public class MockedFixedBank : FixedBank
{
    public MockedFixedBank(int startAddress, int endAddress, string name) 
        : base(startAddress, endAddress, name)
    {
    }

    public override byte ReadByte(ref ushort address)
    {
        if(address == 0xFF44)
        {
            return 0x90;
        }
        
        return base.ReadByte(ref address);
    }

    public override ushort ReadWord(ref ushort address)
    {
        if (address == 0xFF44)
        {
            return 0x9000;
        }
        
        return base.ReadWord(ref address);
    }
}