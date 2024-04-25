namespace GameboyDotnet.Memory.BuildingBlocks;

public class MemoryBankController(int startAddress, int endAddress, string name, int bankSizeInBytes, int numberOfBanks)
    : SwitchableBank(startAddress, endAddress, name, bankSizeInBytes, numberOfBanks)
{
    protected SwitchableBank ExternalRam = new(BankAddress.ExternalRamStart, BankAddress.ExternalRamEnd, nameof(ExternalRam), bankSizeInBytes: 8192, numberOfBanks: 512);
    protected bool ExternalRamEnabled { get; set; }
    protected bool RomBankingMode { get; set; }
    
}