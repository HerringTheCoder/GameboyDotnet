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
            //Neither selected - return all 1s in lower nibble
            else
            {
                _joypadRegister = (byte)(value & 0xF0 | 0x0F);
            }
        }
        
        //Check if audio registers are in read only state (except FF26 - Power Control)
        if (!_apu.IsAudioOn && address is >= 0xFF10 and < 0xFF26)
            return;

        if (address is >= 0xFF30 and <= 0xFF3F)
        {
            _apu.WaveChannel.WriteWaveRam(ref address, ref value);
            return;
        }
        
        switch (address)
        {
            case 0xFF26:
                 _apu.SetPowerState(ref value);
                 break;
            case 0xFF25:
                _apu.SetChannelPanningStates(ref value);
                break;
            case 0xFF24:
                _apu.SetVolumeControlStates(ref value);
                break;
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
        
        // Apply read masks for audio registers (write-only and mixed registers)
        if (address >= 0xFF10 && address <= 0xFF26)
        {
            byte value = base.ReadByte(ref address);
            return (byte)(value | GetAudioRegisterReadMask(address));
        }
        
        return base.ReadByte(ref address);
    }
    
    private byte GetAudioRegisterReadMask(ushort address)
    {
        // Returns the bits that should always read as 1 for write-only/mixed registers
        // Based on Pan Docs and SameBoy's implementation
        return address switch
        {
            // NR1X - Square Channel 1
            0xFF10 => 0x80, // NR10 - bit 7 unused
            0xFF11 => 0x3F, // NR11 - bits 0-5 are write-only (length timer)
            0xFF12 => 0x00, // NR12 - fully readable
            0xFF13 => 0xFF, // NR13 - write-only (period low)
            0xFF14 => 0xBF, // NR14 - bits 0-5 readable, bit 6 readable, bit 7 write-only
            
            // NR2X - Square Channel 2
            0xFF16 => 0x3F, // NR21 - bits 0-5 are write-only (length timer)
            0xFF17 => 0x00, // NR22 - fully readable
            0xFF18 => 0xFF, // NR23 - write-only (period low)
            0xFF19 => 0xBF, // NR24 - bits 0-5 readable, bit 6 readable, bit 7 write-only
            
            // NR3X - Wave Channel
            0xFF1A => 0x7F, // NR30 - bit 7 readable, others unused
            0xFF1B => 0xFF, // NR31 - write-only (length timer)
            0xFF1C => 0x9F, // NR32 - bits 5-6 readable, others unused
            0xFF1D => 0xFF, // NR33 - write-only (period low)
            0xFF1E => 0xBF, // NR34 - bits 0-5 readable, bit 6 readable, bit 7 write-only
            
            // NR4X - Noise Channel
            0xFF20 => 0xFF, // NR41 - write-only (length timer)
            0xFF21 => 0x00, // NR42 - fully readable
            0xFF22 => 0x00, // NR43 - fully readable
            0xFF23 => 0xBF, // NR44 - bit 6 readable, bit 7 write-only, others unused
            
            // NR5X - Master Control
            0xFF24 => 0x00, // NR50 - fully readable
            0xFF25 => 0x00, // NR51 - fully readable
            0xFF26 => 0x70, // NR52 - bit 7 readable, bits 0-3 readable, bits 4-6 unused
            
            _ => 0x00
        };
    }
}