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
        
        //Check if audio registers are in read only state (except FF26 - Power Control)
        if (!_apu.IsAudioOn && address is >= 0xFF10 and < 0xFF26)
            return;

        if (address >= 0xFF30 && address <= 0xFF3F)
        {
            _apu.WaveChannel.WriteWaveRam(ref address, ref value);
            return;
        }
        
        switch (address)
        {
            case 0xFF26:
                 _apu.SetPowerState(ref value);
                 return;
            case 0xFF25:
                _apu.SetChannelPanningStates(ref value);
                return;
            case 0xFF24:
                _apu.SetVolumeControlStates(ref value);
                return; 
            case 0xFF10:
                _apu.SquareChannel1.SetSweepState(ref value);
                break;
            case 0xFF11:
                _apu.SquareChannel1.SetLengthTimer(ref value);
                break;
            case 0xFF12:
                _apu.SquareChannel1.SetVolumeRegister(ref value);
                break;
            case 0xFF13:
                _apu.SquareChannel1.SetPeriodLowOrRandomnessRegister(ref value);
                break;
            case 0xFF14:
                _apu.SquareChannel1.SetPeriodHighControl(ref value);
                break;
            case 0xFF16:
                _apu.SquareChannel2.SetLengthTimer(ref value);
                break;
            case 0xFF17:
                _apu.SquareChannel2.SetVolumeRegister(ref value);
                break;
            case 0xFF18:
                _apu.SquareChannel2.SetPeriodLowOrRandomnessRegister(ref value);
                break;
            case 0xFF19:
                _apu.SquareChannel2.SetPeriodHighControl(ref value);
                break;
            case 0xFF1A:
                _apu.WaveChannel.SetDacStatus(ref value);
                break;
            case 0xFF1B:
                _apu.WaveChannel.SetLengthTimer(ref value);
                break;
            case 0xFF1C:
                _apu.WaveChannel.SetVolumeRegister(ref value);
                break;
            case 0xFF1D:
                _apu.WaveChannel.SetPeriodLowOrRandomnessRegister(ref value);
                break;
            case 0xFF1E:
                _apu.WaveChannel.SetPeriodHighControl(ref value);
                break;
            case 0xFF20:
                _apu.NoiseChannel.SetLengthTimer(ref value);
                break;
            case 0xFF21:
                _apu.NoiseChannel.SetVolumeRegister(ref value);
                break;
            case 0xFF22:
                _apu.NoiseChannel.SetPeriodLowOrRandomnessRegister(ref value);
                break;
            case 0xFF23:
                _apu.NoiseChannel.SetPeriodHighControl(ref value);
                break;
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
        
        if (address >= 0xFF30 && address <= 0xFF3F)
        {
            return _apu.WaveChannel.ReadWaveRam(ref address);
        }
        
        return base.ReadByte(ref address);
    }
}