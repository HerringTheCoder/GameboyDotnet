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
    public SwitchableBank Vram;
    public readonly FixedBank Wram0 = new(BankAddress.Wram0Start, BankAddress.Wram0End, nameof(Wram0));
    public readonly SwitchableBank Wram1;
    public readonly FixedBank Oam = new(BankAddress.OamStart, BankAddress.OamEnd, nameof(Oam));
    public readonly FixedBank NotUsable = new(BankAddress.NotUsableStart, BankAddress.NotUsableEnd, nameof(NotUsable));
    public readonly IoBank IoRegisters;
    public readonly FixedBank HRam = new(BankAddress.HRamStart, BankAddress.HRamEnd, nameof(HRam));

    public readonly FixedBank InterruptEnableRegister = new(BankAddress.InterruptEnableRegisterStart, BankAddress.InterruptEnableRegisterEnd, nameof(InterruptEnableRegister));

    private readonly ILogger<Gameboy> _logger;
    public CgbState CgbState { get; } = new();
    private bool _isInitializing;

    public MemoryController(ILogger<Gameboy> logger, Apu apu)
    {
        _logger = logger;
    _isInitializing = true;
        
        RomBankNn = new Mbc0Mock(nameof(RomBankNn), bankSizeInBytes: 16384, numberOfBanks: 2);
        Vram = new CgbAwareSwitchableBank(BankAddress.VramStart, BankAddress.VramEnd, nameof(Vram), 
            bankSizeInBytes: 8192, numberOfBanks: 2, CgbState, isCgbVram: true);
        Wram1 = new CgbAwareSwitchableBank(BankAddress.Wram1Start, BankAddress.Wram1End, nameof(Wram1), 
            bankSizeInBytes: 4096, numberOfBanks: 8, CgbState, isCgbVram: false);
        IoRegisters = new IoBank(BankAddress.IoRegistersStart, BankAddress.IoRegistersEnd, nameof(IoRegisters), logger, apu, CgbState);
    IoRegisters.SetMemoryController(this);
        InitializeMemoryMap();
        InitializeBootStates();
        
    _isInitializing = false;
    }

    public void LoadProgram(Stream stream)
    {
    // Load first bank to read cartridge header
        var bank0 = new byte[16384];
        var currentPosition = stream.Read(bank0, 0, 16384);
        
    // Detect CGB mode from cartridge header byte 0x143
        byte cgbFlag = bank0[0x143];
        CgbState.CartridgeCgbMode = cgbFlag switch
        {
            0x80 => CgbMode.CgbCompatible,
            0xC0 => CgbMode.CgbOnly,
            _ => CgbMode.DmgOnly
        };
        
        _logger.LogInformation("Cartridge CGB mode: {CgbMode} (0x{CgbFlag:X2})", CgbState.CartridgeCgbMode, cgbFlag);
        
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
    }    public byte ReadByte(ushort address)
    {
        if (address is >= 0xE000 and <= 0xFDFF)
        {
            _logger.LogDebug("Reading from Echo Ram");
            address -= 0x2000;
        }

        var memoryBank = _memoryMap[address];

        return memoryBank.ReadByte(ref address);
    }

    public void WriteByte(ushort address, byte value)
    {
        if (address is >= 0xE000 and <= 0xFDFF)
        {
            _logger.LogDebug("Writing to Echo Ram");
            address -= 0x2000;
        }

        if (address == Constants.DMARegister && !_isInitializing)
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
               
        var sourceBank = _memoryMap[sourceAddress];
        
        for (ushort i = 0; i <= 0x9F; i++)
        {
            var srcAddr = (ushort)(sourceAddress + i);
            var dstAddr = (ushort)(0xFE00 + i);
            
            // Read data from source
            byte data = sourceBank.ReadByte(ref srcAddr);
            
            // Move data directly to OAM
            Oam.WriteByte(ref dstAddr, ref data);
        }
    }
    
    /// <summary>
    /// Performs HDMA transfer (called by IoBank when HDMA5 is written)
    /// </summary>
    public void PerformHdmaTransfer(int bytesToTransfer)
    {
        if (!CgbState.IsCgbEnabled)
            return;
        
        ushort src = CgbState.HdmaSource;
        ushort dst = CgbState.HdmaDestination;
        
        _logger.LogDebug("Performing HDMA transfer: {Bytes} bytes from {Src:X4} to {Dst:X4}", 
            bytesToTransfer, src, dst);
        
        for (int i = 0; i < bytesToTransfer; i++)
        {
            byte data = ReadByte(src);
            
            // Write to VRAM destination
            WriteByte(dst, data);
            
            src++;
            dst++;
        }
        
        // Update the source and destination addresses
        CgbState.HdmaSource = src;
        CgbState.HdmaDestination = dst;
    }
    
    /// <summary>
    /// Performs one block (16 bytes) of HBlank DMA transfer
    /// Should be called during HBlank period
    /// </summary>
    public void PerformHBlankDmaBlock()
    {
        if (!CgbState.IsCgbEnabled || !CgbState.HdmaActive || !CgbState.HdmaIsHBlankMode)
            return;
        
        // Transfer one block (16 bytes)
        PerformHdmaTransfer(16);
        
        // Decrement remaining length
        if (CgbState.HdmaLength > 0)
        {
            CgbState.HdmaLength--;
            
            if (CgbState.HdmaLength == 0)
            {
                // Transfer complete
                CgbState.HdmaActive = false;
                CgbState.HdmaLength = 0xFF;
                _logger.LogDebug("HBlank DMA transfer completed");
            }
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
        bool isCgbMode = CgbState.IsCgbEnabled;
        
        // Common registers (same for DMG and CGB)
        WriteByte(address: 0xFF00, value: 0xCF); // P1/JOYP
        WriteByte(address: 0xFF01, value: 0x00); // SB
        WriteByte(address: 0xFF04, value: 0xAB); // DIV
        WriteByte(address: 0xFF05, value: 0x00); // TIMA
        WriteByte(address: 0xFF06, value: 0x00); // TMA
        WriteByte(address: 0xFF07, value: 0xF8); // TAC
        WriteByte(address: 0xFF0F, value: 0xE1); // IF
        
        // Audio registers (same for DMG and CGB)
        WriteByte(address: 0xFF10, value: 0x80); // NR10
        WriteByte(address: 0xFF11, value: 0xBF); // NR11
        WriteByte(address: 0xFF12, value: 0xF3); // NR12
        WriteByte(address: 0xFF13, value: 0xFF); // NR13
        WriteByte(address: 0xFF14, value: 0xBF); // NR14
        WriteByte(address: 0xFF16, value: 0x3F); // NR21
        WriteByte(address: 0xFF17, value: 0x00); // NR22
        WriteByte(address: 0xFF18, value: 0xFF); // NR23
        WriteByte(address: 0xFF19, value: 0xBF); // NR24
        WriteByte(address: 0xFF1A, value: 0x7F); // NR30
        WriteByte(address: 0xFF1B, value: 0xFF); // NR31
        WriteByte(address: 0xFF1C, value: 0x9F); // NR32
        WriteByte(address: 0xFF1D, value: 0xFF); // NR33
        WriteByte(address: 0xFF1E, value: 0xBF); // NR34
        WriteByte(address: 0xFF20, value: 0xFF); // NR41
        WriteByte(address: 0xFF21, value: 0x00); // NR42
        WriteByte(address: 0xFF22, value: 0x00); // NR43
        WriteByte(address: 0xFF23, value: 0xBF); // NR44
        WriteByte(address: 0xFF24, value: 0x77); // NR50
        WriteByte(address: 0xFF25, value: 0xF3); // NR51
        WriteByte(address: 0xFF26, value: 0xF1); // NR52
        
        // LCD registers
        WriteByte(address: 0xFF40, value: 0x91); // LCDC
        WriteByte(address: 0xFF42, value: 0x00); // SCY
        WriteByte(address: 0xFF43, value: 0x00); // SCX
        WriteByte(address: 0xFF45, value: 0x00); // LYC
        WriteByte(address: 0xFF47, value: 0xFC); // BGP
        WriteByte(address: 0xFF4A, value: 0x00); // WY
        WriteByte(address: 0xFF4B, value: 0x00); // WX
        
        // DMG/CGB-specific registers
        if (isCgbMode)
        {
            // CGB-specific boot states
            WriteByte(address: 0xFF02, value: 0x7F); // SC (CGB: $7F)
            WriteByte(address: 0xFF41, value: 0x85); // STAT (CGB: $85, but timing-dependent)
            WriteByte(address: 0xFF44, value: 0x00); // LY (CGB: $00, but timing-dependent)
            WriteByte(address: 0xFF46, value: 0x00); // DMA (CGB: $00)
            
            // CGB-only registers
            WriteByte(address: 0xFF4D, value: 0x7E); // KEY1 (Speed switch: bit 7=0 normal, bit 0=0 not armed)
            WriteByte(address: 0xFF4F, value: 0xFE); // VBK (VRAM bank, only bit 0 matters)
            WriteByte(address: 0xFF51, value: 0xFF); // HDMA1
            WriteByte(address: 0xFF52, value: 0xFF); // HDMA2
            WriteByte(address: 0xFF53, value: 0xFF); // HDMA3
            WriteByte(address: 0xFF54, value: 0xFF); // HDMA4
            WriteByte(address: 0xFF55, value: 0xFF); // HDMA5
            WriteByte(address: 0xFF56, value: 0x3E); // RP (Infrared)
            WriteByte(address: 0xFF70, value: 0xF8); // SVBK (WRAM bank)
        }
        else
        {
            // DMG-specific boot states
            WriteByte(address: 0xFF02, value: 0x7E); // SC (DMG: $7E)
            WriteByte(address: 0xFF41, value: 0x85); // STAT (DMG: $85)
            WriteByte(address: 0xFF44, value: 0x00); // LY (DMG: $00)
            WriteByte(address: 0xFF46, value: 0xFF); // DMA (DMG: $FF)
        }
        
        // IE register (common)
        WriteByte(address: 0xFFFF, value: 0x00); // IE
    }
} 