using GameboyDotnet.Components.Memory.BuildingBlocks;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Components;

public class MemoryController(ILogger<Gameboy> logger)
{
    public readonly FixedBank RomBank0 = new(){StartAddress = 0x0000, EndAddress = 0x3FFF};
    public SwitchableBank RomBankNn = new() { StartAddress = 0x4000, EndAddress = 0x7FFF, BankSizeInBytes = 16*1024, NumberOfBanks = 512};
    public SwitchableBank Vram = new() { StartAddress = 0x8000, EndAddress = 0x9FFF, BankSizeInBytes = 8*1024, NumberOfBanks = 2};
    public SwitchableBank ExternalRam = new() { StartAddress = 0xA000, EndAddress = 0xBFFF, BankSizeInBytes = 8*1024, NumberOfBanks = 512};
    public readonly FixedBank Wram0 = new() { StartAddress = 0xC000, EndAddress = 0xCFFF};
    public readonly SwitchableBank Wram1 = new() { StartAddress = 0xD000, EndAddress = 0xDFFF, BankSizeInBytes = 4*1024, NumberOfBanks = 8};
    public readonly FixedBank EchoRam = new() { StartAddress = 0xE000, EndAddress = 0xFDFF}; //TODO: Implement the two-sided synchronization
    public readonly FixedBank Oam = new() { StartAddress = 0xFE00, EndAddress = 0xFE9F};
    public readonly FixedBank NotUsable = new() { StartAddress = 0xFEA0, EndAddress = 0xFEFF}; //TODO: Implement the prohibition
    public readonly FixedBank IoRegisters = new() { StartAddress = 0xFF00, EndAddress = 0xFF7F};
    public readonly FixedBank HRam = new() { StartAddress = 0xFF80, EndAddress = 0xFFFE};
    public readonly FixedBank InterruptEnableRegister = new() { StartAddress = 0xFFFF, EndAddress = 0xFFFF};

    public void LoadProgram(Stream stream)
    {
        int bytesRead;
        int currentPosition = 0;
        //TODO: Determine the size and mapper of the ROM, then setup and load the banks accordingly
        
        while ((bytesRead = stream.Read(RomBank0.MemorySpace, 0, RomBank0.MemorySpace.Length - currentPosition)) > 0)
        {
            currentPosition += bytesRead;
        }

        while ((bytesRead = stream.Read(RomBankNn.MemorySpace, 0, RomBankNn.MemorySpace.Length - currentPosition)) > 0)
        {
            currentPosition += bytesRead;
        }
    }

    public byte ReadByte(ushort address)
    {
        var memoryBank = FindMemoryBank(ref address);
        logger.LogDebug("Reading byte from memory address: {address:X} located in {memoryBank.GetType().Name}", address, memoryBank.GetType().Name);
        return memoryBank.ReadByte(ref address);
    }
    
    public void WriteByte(ushort address, byte value)
    {
        var memoryBank = FindMemoryBank(ref address); 
        logger.LogDebug("Writing byte {value:X} to memory address: {address:X} located in {memoryBank.GetType().Name}", value, address, memoryBank.GetType().Name);
        memoryBank.WriteByte(ref address, ref value);
    }
    
    public void WriteWord(ushort address, ushort value)
    {
        var memoryBank = FindMemoryBank(ref address);
        logger.LogDebug("Writing word {value:X} to memory address: {address:X} located in {memoryBank.GetType().Name}", value, address, memoryBank.GetType().Name);
        memoryBank.WriteWord(ref address, ref value);
    }
    
    public ushort ReadWord(ushort address)
    {
        var memoryBank = FindMemoryBank(ref address);
        logger.LogDebug("Reading word from memory address: {address:X} located in {memoryBank.GetType().Name}", address, memoryBank.GetType().Name);
        return memoryBank.ReadWord(ref address);
    }
    
    public void IncrementByte(ushort memoryAddress)
    {
        var memoryBank = FindMemoryBank(ref memoryAddress);
        logger.LogDebug("Incrementing byte at memory address: {memoryAddress:X} located in {memoryBank.GetType().Name}", memoryAddress, memoryBank.GetType().Name);
        memoryBank.IncrementByte(ref memoryAddress);
    }
    
    public void DecrementByte(ushort memoryAddress)
    {
        var memoryBank = FindMemoryBank(ref memoryAddress);
        logger.LogDebug("Decrementing byte at memory address: {memoryAddress:X} located in {memoryBank.GetType().Name}", memoryAddress, memoryBank.GetType().Name);
        memoryBank.DecrementByte(ref memoryAddress);
    }

    private FixedBank FindMemoryBank(ref ushort address)
    {
        return address switch
        {
            _ when address >= RomBank0.StartAddress && address <= RomBank0.EndAddress => RomBank0,
            _ when address >= RomBankNn.StartAddress && address <= RomBankNn.EndAddress => RomBankNn,
            _ when address >= Vram.StartAddress && address <= Vram.EndAddress => Vram,
            _ when address >= ExternalRam.StartAddress && address <= ExternalRam.EndAddress => ExternalRam,
            _ when address >= Wram0.StartAddress && address <= Wram0.EndAddress => Wram0,
            _ when address >= Wram1.StartAddress && address <= Wram1.EndAddress => Wram1,
            _ when address >= EchoRam.StartAddress && address <= EchoRam.EndAddress => EchoRam,
            _ when address >= Oam.StartAddress && address <= Oam.EndAddress => Oam,
            _ when address >= NotUsable.StartAddress && address <= NotUsable.EndAddress => NotUsable,
            _ when address >= IoRegisters.StartAddress && address <= IoRegisters.EndAddress => IoRegisters,
            _ when address >= HRam.StartAddress && address <= HRam.EndAddress => HRam,
            _ when address >= InterruptEnableRegister.StartAddress && address <= InterruptEnableRegister.EndAddress => InterruptEnableRegister,
            _ => throw new ArgumentOutOfRangeException(
                nameof(address),
                $"Could not find a memory bank for the given address: {address:X}")
        };
    }
}