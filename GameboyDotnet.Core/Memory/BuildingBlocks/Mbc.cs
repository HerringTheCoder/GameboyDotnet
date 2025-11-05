namespace GameboyDotnet.Memory.BuildingBlocks;

public class MemoryBankController(string name, int bankSizeInBytes, int numberOfBanks, int ramBankCount)
    : SwitchableBank(BankAddress.RomBank0Start, BankAddress.RomBankNnEnd, name, bankSizeInBytes, numberOfBanks)
{
    public SwitchableBank ExternalRam = new(BankAddress.ExternalRamStart, BankAddress.ExternalRamEnd, nameof(ExternalRam), bankSizeInBytes: 8192, numberOfBanks: ramBankCount > 0 ? ramBankCount : 1);
    protected bool ExternalRamEnabled { get; set; }
    protected int RomBankingMode { get; set; }
    
}