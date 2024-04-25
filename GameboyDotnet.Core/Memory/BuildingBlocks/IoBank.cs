using GameboyDotnet.Extensions;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Memory.BuildingBlocks;

public class IoBank : FixedBank
{
    private readonly ILogger<Gameboy> _logger;
    private byte _interruptEnableRegister = 0x00;
    private byte _interruptFlagRegister = 0x00;
    private byte _lcdcRegister = 0x00;
    private byte _joypadRegister = 0xFF;
    public byte DpadStates = 0xF;
    public byte ButtonStates = 0xF;
    
    public IoBank(int startAddress, int endAddress, string name, ILogger<Gameboy> logger) 
        : base(startAddress, endAddress, name)
    {
        _logger = logger;
    }

    public override void WriteByte(ref ushort address, ref byte value)
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
        if(address == Constants.JoypadRegister)
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

        base.WriteByte(ref address, ref value);
    }

    public override byte ReadByte(ref ushort address)
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
                _logger.LogDebug("ReadByte returning cached value of LCDControlRegister: {value:X}", _joypadRegister);

            return _joypadRegister;
        }
        
        return base.ReadByte(ref address);
    }
}