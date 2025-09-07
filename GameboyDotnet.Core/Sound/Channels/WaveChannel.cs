using GameboyDotnet.Extensions;
using GameboyDotnet.Sound.Channels.BuildingBlocks;

namespace GameboyDotnet.Sound.Channels;

public class WaveChannel(AudioBuffer audioBuffer) : BaseChannel()
{
    public byte WaveVolumeIndex = 0;
    private readonly byte[] _waveRam = new byte[16];
    private readonly byte[] _waveSampleBuffer = new byte[32];
    private int _waveFormCurrentIndex = 0;
    
    public override void Step()
    {
        if (!IsChannelOn)
            return;
        
        var isPeriodTimerFinished = StepPeriodTimer();

        if (isPeriodTimerFinished)
        {
            _waveFormCurrentIndex = (_waveFormCurrentIndex + 1) & 0b1111; //Wrap after 15
            RefreshOutputState();
        }
    }

    protected override void RefreshOutputState()
    {
        //This effectively maps WaveVolumeIndex to proper multipliers [0f, 1f, 0.5f, 0.25f] while avoiding multiplication
        CurrentOutput = IsChannelOn && WaveVolumeIndex != 0
            ? _waveSampleBuffer[_waveFormCurrentIndex] >> (WaveVolumeIndex - 1)
            : 0;
    }

    protected override void ResetLengthTimerValue()
    {
        LengthTimer = 256;
    }

    public void SetDacStatus(ref byte value)
    {
        IsDacEnabled = value.IsBitSet(7);
        if (!IsDacEnabled)
        {
            IsChannelOn = false;
        }
    }

    public override void SetVolumeRegister(ref byte value)
    {
        WaveVolumeIndex = (byte)((value & 0b0110_0000) >> 5);
    }

    protected override void ResetPeriodTimer()
    {
        PeriodTimer = (2048 - GetPeriodValueFromRegisters) * 2;
    }

    public override void Reset()
    {
        VolumeLevel = 0;
        _waveFormCurrentIndex = 0;
        base.Reset();
    }

    public void WriteWaveRam(ref ushort address, ref byte value)
    {
        var ramAddress = address - 0xFF30;
        _waveRam[ramAddress] = value;
        
        //Wave byte contains 2 samples (4 bits each), extract upper and lower 4 bits
        _waveSampleBuffer[ramAddress * 2] = (byte)((value & 0b1111_0000) >> 4);
        _waveSampleBuffer[ramAddress * 2 + 1] = (byte)(value & 0b0000_1111);
    }

    public byte ReadWaveRam(ref ushort address)
    {
        var ramAddress = address - 0xFF30;
        return _waveRam[ramAddress];
    }

    public override void SetLengthTimer(ref byte value)
    {
        InitialLengthTimer = value;
        LengthTimer = 256 - InitialLengthTimer;
    }
}