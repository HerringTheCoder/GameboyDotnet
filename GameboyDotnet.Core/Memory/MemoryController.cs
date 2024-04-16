using System.Text;
using GameboyDotnet.Extensions;
using GameboyDotnet.Memory.BuildingBlocks;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Components;

public class MemoryController
{
    public readonly FixedBank RomBank0 = new(0x0000, 0x3FFF, nameof(RomBank0));
    public SwitchableBank RomBankNn = new(0x4000, 0x7FFF, nameof(RomBankNn), bankSizeInBytes: 16*1024, numberOfBanks: 512);
    public SwitchableBank Vram = new(0x8000, 0x9FFF, nameof(Vram), bankSizeInBytes: 8*1024, numberOfBanks:2);
    public SwitchableBank ExternalRam = new(0xA000, 0xBFFF, nameof(ExternalRam), bankSizeInBytes:8*1024, numberOfBanks:512);
    public readonly FixedBank Wram0 = new(0xC000, 0xCFFF, nameof(Wram0));
    public readonly SwitchableBank Wram1 = new(0xD000, 0xDFFF, nameof(Wram1), bankSizeInBytes: 4*1024, numberOfBanks: 8);
    // public readonly FixedBank EchoRam = new(0xE000, 0xFDFF, nameof(EchoRam));
    public readonly FixedBank Oam = new(0xFE00, 0xFE9F, nameof(Oam));
    public readonly FixedBank NotUsable = new(0xFEA0, 0xFEFF, nameof(NotUsable));
    public readonly FixedBank IoRegisters = new(0xFF00, 0xFF7F, nameof(IoRegisters));
    public readonly FixedBank HRam = new(0xFF80, 0xFFFE, nameof(HRam));
    public readonly FixedBank InterruptEnableRegister = new(0xFFFF, 0xFFFF, nameof(InterruptEnableRegister));

    private StringBuilder _sb = new();
    private readonly ILogger<Gameboy> _logger;
    
    public MemoryController(ILogger<Gameboy> logger, bool isTestEnvironment)
    {
        _logger = logger;
        
        if (isTestEnvironment)
        {
            IoRegisters = new MockedFixedBank(0xFF00, 0xFF7F, nameof(IoRegisters));
        }
        
        WriteByte(0xFF4D, 0xFF); //TODO: Implement the double speed switch
        WriteByte(0xFF00, 0xFF);
    }
    
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
        _logger.LogDebug("Reading byte from memory address: {address:X} located in {memoryBank.Name}", address, memoryBank.Name);
        return memoryBank.ReadByte(ref address);
    }
    
    public void WriteByte(ushort address, byte value)
    {
        // if (address <= 0x7FFF)
        // {
        //     return; //TODO: MBC0 behavior only
        // }
        if (address is 0xFF01)
        {
            Console.WriteLine($"SB WRITE: {value:X2}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("-----------------");
            _sb.Append((char)value);
            Console.WriteLine(_sb);
            Console.WriteLine("-----------------");
            Console.ResetColor();
            if (_sb.ToString().Contains("Passed\n"))
            {
                throw new Exception("Test Passed");
            }
        }

        var memoryBank = FindMemoryBank(ref address);
        
        _logger.LogDebug("Writing byte {value:X} to memory address: {address:X} located in {memoryBank.Name}", value, address, memoryBank.Name);
        memoryBank.WriteByte(ref address, ref value);
    }
    
    public void WriteWord(ushort address, ushort value)
    {
        var memoryBank = FindMemoryBank(ref address);
        _logger.LogDebug("Writing word {value:X} to memory address: {address:X} located in {memoryBank.Name}", value, address, memoryBank.Name);
       
        WriteByte(address, (byte)(value & 0xFF));
        WriteByte(address.Add(1), (byte)(value >> 8));
    }
    
    public ushort ReadWord(ushort address)
    {
        var memoryBank = FindMemoryBank(ref address);
        _logger.LogDebug("Reading word from memory address: {address:X} located in {memoryBank.Name}", address, memoryBank.Name);
        return (ushort)(ReadByte(address) | (ReadByte(address.Add(1)) << 8));
    }
    
    public void IncrementByte(ushort memoryAddress)
    {
        var memoryBank = FindMemoryBank(ref memoryAddress);
        _logger.LogDebug("Incrementing byte at memory address: {memoryAddress:X} located in {memoryBank.Name}", memoryAddress, memoryBank.Name);
        memoryBank.IncrementByte(ref memoryAddress);
    }
    
    public void DecrementByte(ushort memoryAddress)
    {
        var memoryBank = FindMemoryBank(ref memoryAddress);
        _logger.LogDebug("Decrementing byte at memory address: {memoryAddress:X} located in {memoryBank.Name}", memoryAddress, memoryBank.Name);
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
            _ when address >= 0xC000 && address <= 0xCFFF => Wram0, //Echo of Wram0
            _ when address >= 0xD000 && address <= 0xFDFF => Wram1, //Echo of Wram1
            // _ when address >= EchoRam.StartAddress && address <= EchoRam.EndAddress => EchoRam,
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