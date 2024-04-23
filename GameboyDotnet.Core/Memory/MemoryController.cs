using System.Text;
using GameboyDotnet.Extensions;
using GameboyDotnet.Memory.BuildingBlocks;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Memory;

public class MemoryController
{
    public readonly FixedBank RomBank0 = new(BankAddress.RomBank0Start, BankAddress.RomBank0End, nameof(RomBank0));

    public SwitchableBank RomBankNn = new(BankAddress.RomBankNnStart, BankAddress.RomBankNnEnd, nameof(RomBankNn), bankSizeInBytes: 16 * 1024,
        numberOfBanks: 512);

    public SwitchableBank Vram = new(BankAddress.VramStart, BankAddress.VramEnd, nameof(Vram), bankSizeInBytes: 8 * 1024, numberOfBanks: 2);

    public SwitchableBank ExternalRam =
        new(BankAddress.ExternalRamStart, BankAddress.ExternalRamEnd, nameof(ExternalRam), bankSizeInBytes: 8 * 1024, numberOfBanks: 512);

    public readonly FixedBank Wram0 = new(BankAddress.Wram0Start, BankAddress.Wram0End, nameof(Wram0));

    public readonly SwitchableBank Wram1 = new(BankAddress.Wram1Start,BankAddress.Wram1End, nameof(Wram1), bankSizeInBytes: 4 * 1024,
        numberOfBanks: 8);

    // public readonly FixedBank EchoRam = new(0xE000, 0xFDFF, nameof(EchoRam));
    public readonly FixedBank Oam = new(BankAddress.OamStart, BankAddress.OamEnd, nameof(Oam));
    public readonly FixedBank NotUsable = new(BankAddress.NotUsableStart, BankAddress.NotUsableEnd, nameof(NotUsable));
    public readonly FixedBank IoRegisters = new(BankAddress.IoRegistersStart, BankAddress.IoRegistersEnd, nameof(IoRegisters));
    public readonly FixedBank HRam = new(BankAddress.HRamStart, BankAddress.HRamEnd, nameof(HRam));
    public readonly FixedBank InterruptEnableRegister = new(BankAddress.InterruptEnableRegisterStart, BankAddress.InterruptEnableRegisterEnd, nameof(InterruptEnableRegister));
    //Cache for read hotspots
    private byte _interruptEnableRegister = 0x00;
    private byte _interruptFlagRegister = 0x00;
    private byte _lcdcRegister = 0x00;
    internal byte _joypadRegister = 0xFF;
    public byte DpadStates = 0xF;
    public byte ButtonStates = 0xF;

    private StringBuilder _sb = new();
    private readonly ILogger<Gameboy> _logger;

    public MemoryController(ILogger<Gameboy> logger, bool isTestEnvironment)
    {
        _logger = logger;

        if (isTestEnvironment)
        {
            IoRegisters = new MockedFixedBank(0xFF00, 0xFF7F, nameof(IoRegisters));
        }
        
        InitializeMemoryMap();

        WriteByte(Constants.JoypadRegister, 0xCF);
        
        WriteByte(0xFF02, 0x7E);
        WriteByte(0xFF04, 0xAB);//18?
        WriteByte(0xFF07, 0xF8);
        WriteByte(0xFF0F, 0xE1);
        WriteByte(0xFF10, 0x80);
        WriteByte(0xFF11, 0xBF);
        WriteByte(0xFF12, 0xF3);
        WriteByte(0xFF13, 0xFF);
        WriteByte(0xFF14, 0xBF);
        WriteByte(0xFF16, 0x3F);
        WriteByte(0xFF18, 0xFF);
        WriteByte(0xFF19, 0xBF);
        WriteByte(0xFF1A, 0x7F);
        WriteByte(0xFF1B, 0xFF);
        WriteByte(0xFF1C, 0x9F);
        WriteByte(0xFF1D, 0xFF);
        WriteByte(0xFF1E, 0xBF);
        WriteByte(0xFF20, 0xFF);
        WriteByte(0xFF23, 0xBF);
        WriteByte(0xFF24, 0x77);
        WriteByte(0xFF25, 0xF3);
        WriteByte(0xFF26, 0xF1);
        WriteByte(0xFF40, 0x91);
        WriteByte(0xFF41, 0x01);
        WriteByte(0xFF44, 0x90);
        WriteByte(0xFF47, 0xFC);
        WriteByte(0xFF4D, 0xFF); //TODO: Implement the double speed switch
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
        if (address == Constants.IERegister)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("ReadByte returning cached value of InterruptEnableRegister: {value:X}", _interruptEnableRegister);
            
            return _interruptEnableRegister;
        }

        if (address == Constants.IFRegister)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("ReadByte returning cached value of InterruptFlagRegister: {value:X}", _interruptFlagRegister);

            return _interruptFlagRegister;
        }

        if (address == Constants.LCDControlRegister)
        {
            if(_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("ReadByte returning cached value of LCDControlRegister: {value:X}", _lcdcRegister);
            
            return _lcdcRegister;
        }
        
        if(address == 0xFF00)
        {
            if(_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("ReadByte returning cached value of LCDControlRegister: {value:X}", _lcdcRegister);

            return _joypadRegister;
        }
        
        var memoryBank = _memoryMap[address];
        
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Reading byte from memory address: {address:X} located in {memoryBank.Name}", address,
                memoryBank.Name);
        
        return memoryBank.ReadByte(ref address);
    }

    public void WriteByte(ushort address, byte value)
    {
        if (address == Constants.IERegister)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("WriteByte refreshing InterruptEnable cache with value: {value:X}", value);

            _interruptEnableRegister = value;
        }
        
        if (address == Constants.IFRegister)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("WriteByte refreshing InterruptFlag cache with value: {value:X}", value);

            _interruptFlagRegister = value;
        }

        if (address == Constants.LCDControlRegister)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("WriteByte refreshing LCDControlRegister cache with value: {value:X}", value);
            
            _lcdcRegister = value;
        }
        
        if(address == 0xFF00)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("WriteByte intercepted JoypadRegister write with value: {value:X}", value);
            
            if((value & 0xF0) == (_joypadRegister & 0xF0))
                return;
            
            //Dpad selected
            if (!value.IsBitSet(4))
            {
                _joypadRegister = (byte)(value & 0xF0 | (byte)(DpadStates & 0x0F));
            }
            //Buttons selected
            else if (!value.IsBitSet(5))
            {
                _joypadRegister = (byte)(value & 0xF0 | (byte)(ButtonStates & 0x0F));
            }
        }
        
        if (address <= 0x7FFF)
        {
            return; //TODO: MBC0 behavior only
        }
        
        if (address is 0xFF01 && _logger.IsEnabled(LogLevel.Debug))
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
                // throw new Exception("Test Passed");
            }
        }

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
    
    private readonly FixedBank[] _memoryMap = new FixedBank[0xFFFF + 1];
    
    private void InitializeMemoryMap()
    {
        for (int i = 0; i <= 0xFFFF; i++)
        {
            _memoryMap[i] = i switch
            {
                <= BankAddress.RomBank0End => RomBank0,
                <= BankAddress.RomBankNnEnd => RomBankNn,
                <= BankAddress.VramEnd => Vram,
                <= BankAddress.ExternalRamEnd => ExternalRam,
                <= BankAddress.Wram0End => Wram0,
                <= BankAddress.Wram1End => Wram1,
                <= 0xEFFF => Wram0, //Echo of Wram0
                <= 0xFDFF => Wram1, //Echo of Wram1
                // _ when address >= EchoRamStartAddress && address <= EchoRam.EndAddress => EchoRam,
                <= BankAddress.OamEnd => Oam,
                <= BankAddress.NotUsableEnd => NotUsable,
                <= BankAddress.IoRegistersEnd => IoRegisters,
                <= BankAddress.HRamEnd => HRam,
                _ => InterruptEnableRegister
            };
        }
    }
}