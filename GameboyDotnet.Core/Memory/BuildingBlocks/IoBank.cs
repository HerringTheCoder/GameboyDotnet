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
    private CgbState _cgbState;
    private MemoryController? _memoryController; // Will be set after construction
    
    public IoBank(int startAddress, int endAddress, string name, ILogger<Gameboy> logger, Apu apu, CgbState cgbState) 
        : base(startAddress, endAddress, name)
    {
        _apu = apu;
        _logger = logger;
        _cgbState = cgbState;
    }
    
    public void SetMemoryController(MemoryController memoryController)
    {
        _memoryController = memoryController;
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
            
            // CGB-specific registers
            case Constants.KEY1Register:
                if (_cgbState.IsCgbEnabled)
                {
                    // Only bit 0 is writable (speed switch armed flag)
                    _cgbState.SpeedSwitchArmed = (value & 0x01) != 0;
                    return;
                }
                break;
                
            case Constants.VBKRegister:
                if (_cgbState.IsCgbEnabled)
                {
                    _cgbState.CurrentVramBank = (byte)(value & 0x01);
                    _logger.LogDebug("VRAM bank switched to: {Bank}", _cgbState.CurrentVramBank);
                    return;
                }
                break;
                
            case Constants.HDMA1Register:
                if (_cgbState.IsCgbEnabled)
                {
                    _cgbState.HdmaSource = (ushort)((_cgbState.HdmaSource & 0x00FF) | ((value & 0xFF) << 8));
                }
                break;
                
            case Constants.HDMA2Register:
                if (_cgbState.IsCgbEnabled)
                {
                    _cgbState.HdmaSource = (ushort)((_cgbState.HdmaSource & 0xFF00) | (value & 0xF0));
                }
                break;
                
            case Constants.HDMA3Register:
                if (_cgbState.IsCgbEnabled)
                {
                    _cgbState.HdmaDestination = (ushort)(0x8000 | (((value & 0x1F) << 8) | (_cgbState.HdmaDestination & 0x00FF)));
                }
                break;
                
            case Constants.HDMA4Register:
                if (_cgbState.IsCgbEnabled)
                {
                    _cgbState.HdmaDestination = (ushort)((_cgbState.HdmaDestination & 0xFF00) | (value & 0xF0));
                }
                break;
                
            case Constants.HDMA5Register:
                if (_cgbState.IsCgbEnabled)
                {
                    HandleHdmaTransfer(ref value);
                    return;
                }
                break;
                
            case Constants.BCPSRegister:
                if (_cgbState.IsCgbEnabled)
                {
                    _cgbState.BackgroundPaletteIndex = value;
                }
                break;
                
            case Constants.BCPDRegister:
                if (_cgbState.IsCgbEnabled)
                {
                    byte index = (byte)(_cgbState.BackgroundPaletteIndex & 0x3F);
                    _cgbState.BackgroundPaletteMemory[index] = value;
                    
                    if ((_cgbState.BackgroundPaletteIndex & 0x80) != 0)
                    {
                        _cgbState.BackgroundPaletteIndex = (byte)(0x80 | ((index + 1) & 0x3F));
                    }
                    return;
                }
                break;
                
            case Constants.OCPSRegister:
                if (_cgbState.IsCgbEnabled)
                {
                    _cgbState.ObjectPaletteIndex = value;
                }
                break;
                
            case Constants.OCPDRegister:
                if (_cgbState.IsCgbEnabled)
                {
                    byte index = (byte)(_cgbState.ObjectPaletteIndex & 0x3F);
                    _cgbState.ObjectPaletteMemory[index] = value;
                    
                    if ((_cgbState.ObjectPaletteIndex & 0x80) != 0)
                    {
                        _cgbState.ObjectPaletteIndex = (byte)(0x80 | ((index + 1) & 0x3F));
                    }
                    return;
                }
                break;
                
            case Constants.OPRIRegister:
                if (_cgbState.IsCgbEnabled)
                {
                    _cgbState.DmgStyleObjectPriority = (value & 0x01) != 0;
                }
                break;
                
            case Constants.SVBKRegister:
                if (_cgbState.IsCgbEnabled)
                {
                    byte bank = (byte)(value & 0x07);
                    _cgbState.CurrentWramBank = bank == 0 ? (byte)1 : bank;
                    _logger.LogDebug("WRAM bank switched to: {Bank}", _cgbState.CurrentWramBank);
                    return;
                }
                break;
                
            default:
                break;
        }
        
        base.WriteByte(ref address, ref value);
    }
    
    private void HandleHdmaTransfer(ref byte value)
    {
        bool isHBlankMode = (value & 0x80) != 0;
        byte length = (byte)(value & 0x7F);
        
        if (_cgbState.HdmaActive && !isHBlankMode)
        {
            _cgbState.HdmaActive = false;
            _cgbState.HdmaLength = (byte)(length | 0x80);
            _logger.LogDebug("HBlank DMA terminated, remaining length: {Length}", length);
            return;
        }
        
        _cgbState.HdmaLength = length;
        _cgbState.HdmaIsHBlankMode = isHBlankMode;
        
        if (isHBlankMode)
        {
            _cgbState.HdmaActive = true;
            _logger.LogDebug("HBlank DMA started: source={Source:X4}, dest={Dest:X4}, length={Length}", 
                _cgbState.HdmaSource, _cgbState.HdmaDestination, length);
        }
        else
        {
            PerformGeneralPurposeDma(length);
        }
    }
    
    private void PerformGeneralPurposeDma(byte length)
    {
        int bytesToTransfer = (length + 1) * 16;
        
        _logger.LogDebug("General Purpose DMA: source={Source:X4}, dest={Dest:X4}, bytes={Bytes}", 
            _cgbState.HdmaSource, _cgbState.HdmaDestination, bytesToTransfer);
        
        if (_memoryController != null)
        {
            _memoryController.PerformHdmaTransfer(bytesToTransfer);
        }
        
        _cgbState.HdmaLength = 0xFF;
        _cgbState.HdmaActive = false;
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
        
        // CGB-specific register reads
        switch (address)
        {
            case Constants.KEY1Register:
                if (_cgbState.IsCgbEnabled)
                {
                    byte speedBit = Cycles.CgbDoubleSpeedMode ? (byte)0x80 : (byte)0x00;
                    byte armedBit = _cgbState.SpeedSwitchArmed ? (byte)0x01 : (byte)0x00;
                    return (byte)(speedBit | armedBit | 0x7E);
                }
                break;
                
            case Constants.VBKRegister:
                if (_cgbState.IsCgbEnabled)
                {
                    return (byte)(_cgbState.CurrentVramBank | 0xFE);
                }
                return 0xFF;
                
            case Constants.HDMA5Register:
                if (_cgbState.IsCgbEnabled)
                {
                    if (_cgbState.HdmaActive)
                    {
                        return (byte)(_cgbState.HdmaLength & 0x7F);
                    }
                    else
                    {
                        return 0xFF;
                    }
                }
                break;
                
            case Constants.BCPSRegister:
                if (_cgbState.IsCgbEnabled)
                {
                    return _cgbState.BackgroundPaletteIndex;
                }
                break;
                
            case Constants.BCPDRegister:
                if (_cgbState.IsCgbEnabled)
                {
                    byte index = (byte)(_cgbState.BackgroundPaletteIndex & 0x3F);
                    return _cgbState.BackgroundPaletteMemory[index];
                }
                break;
                
            case Constants.OCPSRegister:
                if (_cgbState.IsCgbEnabled)
                {
                    return _cgbState.ObjectPaletteIndex;
                }
                break;
                
            case Constants.OCPDRegister:
                if (_cgbState.IsCgbEnabled)
                {
                    byte index = (byte)(_cgbState.ObjectPaletteIndex & 0x3F);
                    return _cgbState.ObjectPaletteMemory[index];
                }
                break;
                
            case Constants.OPRIRegister:
                if (_cgbState.IsCgbEnabled)
                {
                    return (byte)(_cgbState.DmgStyleObjectPriority ? 0x01 : 0x00);
                }
                break;
                
            case Constants.SVBKRegister:
                if (_cgbState.IsCgbEnabled)
                {
                    return _cgbState.CurrentWramBank;
                }
                return 0xFF;
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