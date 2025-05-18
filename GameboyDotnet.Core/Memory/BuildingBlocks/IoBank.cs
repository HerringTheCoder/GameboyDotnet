using GameboyDotnet.Extensions;
using GameboyDotnet.Sound;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Memory.BuildingBlocks;

public class IoBank : FixedBank
{
    private readonly ILogger<Gameboy> _logger;
    private byte _joypadRegister = 0xFF;
    public byte DpadStates = 0xF;
    public byte ButtonStates = 0xF;
    private Apu _apu;
    
    public IoBank(int startAddress, int endAddress, string name, ILogger<Gameboy> logger, Apu apu) 
        : base(startAddress, endAddress, name)
    {
        _apu = apu;
        _logger = logger;
    }

    public override void WriteByte(ref ushort address, ref byte value)
    {
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
        
        switch (address)
        {
            case 0xFF26:
                 _apu.UpdatePowerState(ref value);
                 return;
            break;
            case 0xFF25:
                _apu.UpdateChannelPanningStates(ref value);
                return;
            case 0xFF24:
                _apu.UpdateVolumeControlStates(ref value);
                return; 
            case 0xFF10:
                //TODO: Channel 1 Sweep
                break;
            case 0xFF11:
                //TODO: Channel 1 length/duty
                break;
            case 0xFF12:
                //TODO: Channel 1 volume & envelope
                break;
            case 0xFF13:
                //TODO: Channel 1 period low
                break;
            case 0xFF14:
                //TODO: Channel 1 period high & control
                break;
            case 0xFF16:
                _apu.SquareChannel2.UpdateLengthDuty(ref value);
                return;
            case 0xFF17:
                _apu.SquareChannel2.UpdateVolumeEnvelope(ref value);
                return;
            case 0xFF18:
                _apu.SquareChannel2.UpdatePeriodLow(ref value);
                return;
            case 0xFF19:
                _apu.SquareChannel2.UpdatePeriodHighControl(ref value);
                return;
            default:
                break;
        }
        
        base.WriteByte(ref address, ref value);
    }

    public override byte ReadByte(ref ushort address)
    {
        if(address == 0xFF00)
        {
            if(_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("ReadByte returning cached value of Joypad register: {value:X}", _joypadRegister);
        
            return _joypadRegister;
        }
        
        return base.ReadByte(ref address);
    }
}