﻿namespace GameboyDotnet.Memory.BuildingBlocks;

public class MemoryBankController(string name, int bankSizeInBytes, int numberOfBanks)
    : SwitchableBank(BankAddress.RomBank0Start, BankAddress.RomBankNnEnd, name, bankSizeInBytes, numberOfBanks)
{
    protected SwitchableBank ExternalRam = new(BankAddress.ExternalRamStart, BankAddress.ExternalRamEnd, nameof(ExternalRam), bankSizeInBytes: 8192, numberOfBanks: 512);
    protected bool ExternalRamEnabled { get; set; }
    protected bool RomBankingMode { get; set; }
    
}