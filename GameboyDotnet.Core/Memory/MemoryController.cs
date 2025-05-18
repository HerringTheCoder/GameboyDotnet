using System.Text;
using GameboyDotnet.Extensions;
using GameboyDotnet.Memory.BuildingBlocks;
using GameboyDotnet.Memory.Mbc;
using GameboyDotnet.Sound;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Memory;

public class MemoryController
{
    private readonly FixedBank[] _memoryMap = new FixedBank[0xFFFF + 1];
    public SwitchableBank RomBankNn;
    public SwitchableBank Vram = new(BankAddress.VramStart, BankAddress.VramEnd, nameof(Vram), bankSizeInBytes: 8192, numberOfBanks: 2);
    public readonly FixedBank Wram0 = new(BankAddress.Wram0Start, BankAddress.Wram0End, nameof(Wram0));
    public readonly SwitchableBank Wram1 = new(BankAddress.Wram1Start, BankAddress.Wram1End, nameof(Wram1), bankSizeInBytes: 4096, numberOfBanks: 8);
    public readonly FixedBank Oam = new(BankAddress.OamStart, BankAddress.OamEnd, nameof(Oam));
    public readonly FixedBank NotUsable = new(BankAddress.NotUsableStart, BankAddress.NotUsableEnd, nameof(NotUsable));
    public readonly IoBank IoRegisters;
    public readonly FixedBank HRam = new(BankAddress.HRamStart, BankAddress.HRamEnd, nameof(HRam));

    public readonly FixedBank InterruptEnableRegister = new(BankAddress.InterruptEnableRegisterStart, BankAddress.InterruptEnableRegisterEnd, nameof(InterruptEnableRegister));

    private readonly ILogger<Gameboy> _logger;

    public MemoryController(ILogger<Gameboy> logger, Apu apu)
    {
        _logger = logger;
        RomBankNn = new Mbc0Mock(nameof(RomBankNn), bankSizeInBytes: 16384, numberOfBanks: 2);
        IoRegisters = new IoBank(BankAddress.IoRegistersStart, BankAddress.IoRegistersEnd, nameof(IoRegisters), logger, apu);
        InitializeMemoryMap();
        InitializeBootStates();
    }

    public void LoadProgram(Stream stream)
    {
        //Load first bank to read cartridge header
        var bank0 = new byte[16384];
        var currentPosition = stream.Read(bank0, 0, 16384);
        RomBankNn = MbcFactory.CreateMbc(cartridgeType: bank0[0x147], romSizeByte: bank0[0x148], ramSize: bank0[0x149]);
        InitializeMemoryMap();

        //Load the first bank
        bank0.CopyTo(RomBankNn.MemorySpace, 0);
        //Load the rest of the banks
        for (int i = 1; i < RomBankNn.NumberOfBanks; i++)
        {
            int bytesRead;
            while ((bytesRead = stream.Read(
                       buffer: RomBankNn.MemorySpace,
                       offset: RomBankNn.BankSizeInBytes * i,
                       count: RomBankNn.BankSizeInBytes * (i + 1) - currentPosition)) > 0)
            {
                currentPosition += bytesRead;
            }

            if (stream.Length == currentPosition)
                break;
        }
    }
    
    public byte ReadByte(ushort address)
    {
        if (address is >= 0xE000 and <= 0xFDFF)
        {
            _logger.LogDebug("Reading from Echo Ram");
            address -= 0x2000; //Adjust address for Echo Ram -> Wram
        }

        var memoryBank = _memoryMap[address];

        return memoryBank.ReadByte(ref address);
    }

    public void WriteByte(ushort address, byte value)
    {
        if (address is >= 0xE000 and <= 0xFDFF)
        {
            _logger.LogDebug("Writing to Echo Ram");
            address -= 0x2000; //Adjust address for Echo Ram -> Wram
        }

        if (address == Constants.DMARegister)
        {
            DmaTransfer(ref value);
        }

// #if DEBUG
//         if (address is 0xFF01 && _logger.IsEnabled(LogLevel.Debug))
//         {
//             Console.WriteLine($"SB WRITE: {value:X2}");
//             Console.ForegroundColor = ConsoleColor.Green;
//             Console.WriteLine("-----------------");
//             _sb.Append((char)value);
//             Console.WriteLine(_sb);
//             Console.WriteLine("-----------------");
//             Console.ResetColor();
//             if (_sb.ToString().Contains("Passed\n"))
//             {
//                 // throw new Exception("Test Passed");
//             }
//         }
// #endif

        var memoryBank = _memoryMap[address];

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Writing byte {value:X} to memory address: {address:X} located in {memoryBank.Name}",
                value, address, memoryBank.Name);

        memoryBank.WriteByte(ref address, ref value);
    }

    public void WriteWord(ushort address, ushort value)
    {
        WriteByte(address, (byte)(value & 0xFF));
        WriteByte(address.Add(1), (byte)(value >> 8));
    }

    public ushort ReadWord(ushort address)
    {
        return (ushort)(ReadByte(address) | (ReadByte(address.Add(1)) << 8));
    }

    public void IncrementByte(ushort address)
    {
        var memoryBank = _memoryMap[address];

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Incrementing byte at memory address: {memoryAddress:X} located in {memoryBank.Name}",
                address, memoryBank.Name);

        memoryBank.IncrementByte(ref address);
    }

    public void DecrementByte(ushort address)
    {
        var memoryBank = _memoryMap[address];

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Decrementing byte at memory address: {memoryAddress:X} located in {memoryBank.Name}",
                address, memoryBank.Name);

        memoryBank.DecrementByte(ref address);
    }

    private void DmaTransfer(ref byte value)
    {
        ushort sourceAddress = (ushort)(value << 8); //0x_XX00
        for (ushort i = 0xFE00; i <= 0xFE9F; i++)
        {
            WriteByte(i, ReadByte(sourceAddress++));
        }
    }


    private void InitializeMemoryMap()
    {
        for (int i = 0; i <= 65535; i++)
        {
            _memoryMap[i] = i switch
            {
                <= BankAddress.RomBankNnEnd => RomBankNn,
                <= BankAddress.VramEnd => Vram,
                <= BankAddress.ExternalRamEnd => RomBankNn,
                <= BankAddress.Wram0End => Wram0,
                <= BankAddress.Wram1End => Wram1,
                <= 0xEFFF => Wram0, //Echo of Wram0
                <= 0xFDFF => Wram1, //Echo of Wram1
                <= BankAddress.OamEnd => Oam,
                <= BankAddress.NotUsableEnd => NotUsable,
                <= BankAddress.IoRegistersEnd => IoRegisters,
                <= BankAddress.HRamEnd => HRam,
                _ => InterruptEnableRegister
            };
        }
    }

    private void InitializeBootStates()
    {
        WriteByte(address: Constants.JoypadRegister, value: 0xCF);
        WriteByte(address: 0xFF02, value: 0x7E);
        WriteByte(address: 0xFF04, value: 0xAB);
        WriteByte(address: 0xFF07, value: 0xF8);
        WriteByte(address: 0xFF0F, value: 0xE1);
        WriteByte(address: 0xFF10, value: 0x80);
        WriteByte(address: 0xFF11, value: 0xBF);
        WriteByte(address: 0xFF12, value: 0xF3);
        WriteByte(address: 0xFF13, value: 0xFF);
        WriteByte(address: 0xFF14, value: 0xBF);
        WriteByte(address: 0xFF16, value: 0x3F);
        WriteByte(address: 0xFF18, value: 0xFF);
        WriteByte(address: 0xFF19, value: 0xBF);
        WriteByte(address: 0xFF1A, value: 0x7F);
        WriteByte(address: 0xFF1B, value: 0xFF);
        WriteByte(address: 0xFF1C, value: 0x9F);
        WriteByte(address: 0xFF1D, value: 0xFF);
        WriteByte(address: 0xFF1E, value: 0xBF);
        WriteByte(address: 0xFF20, value: 0xFF);
        WriteByte(address: 0xFF23, value: 0xBF);
        WriteByte(address: 0xFF24, value: 0x77);
        WriteByte(address: 0xFF25, value: 0xF3);
        WriteByte(address: 0xFF26, value: 0xF1);
        WriteByte(address: 0xFF40, value: 0x91);
        WriteByte(address: 0xFF41, value: 0x01);
        WriteByte(address: 0xFF44, value: 0x90);
        WriteByte(address: 0xFF47, value: 0xFC);
        WriteByte(address: 0xFF4D, value: 0xFF);
    }
} 