using GameboyDotnet.Sound.Channels.BuildingBlocks;

namespace GameboyDotnet.Sound.Channels;

public class NoiseChannel() : BaseChannel()
{
    private ushort _lfsr = 0xFFFF; // 15-bit Linear Feedback Shift Register, initialized to all 1s
    private byte _clockShift;      // NR43 bits 7-4: Clock shift (0-15)
    private bool _widthMode;       // NR43 bit 3: Width mode (0 = 15-bit, 1 = 7-bit)
    private byte _divisorCode;     // NR43 bits 2-0: Divisor code (0-7)
    
    private static readonly int[] DivisorTable = [8, 16, 32, 48, 64, 80, 96, 112];
    
    public override void Step()
    {
        if (!IsChannelOn)
            return;

        var isPeriodTimerFinished = StepPeriodTimer();

        if (isPeriodTimerFinished)
        {
            // Clock the LFSR when timer expires
            ClockLfsr();
            RefreshOutputState();
        }
    }

    //https://gbdev.gg8.se/wiki/articles/Gameboy_sound_hardware#Noise_Channel
    private void ClockLfsr()
    {
        // Per docs: Using a noise channel clock shift of 14 or 15 results in the LFSR receiving no clocks
        if (_clockShift >= 14)
            return;

        // XOR the low two bits (0 and 1)
        int xorResult = (_lfsr & 0x1) ^ ((_lfsr >> 1) & 0x1);

        // Shift all bits right by one
        _lfsr >>= 1;

        // Put XOR result into the now-empty high bit (bit 14)
        if (xorResult == 1)
            _lfsr |= 0x4000;

        // If width mode is 1, put XOR result into bit 6 (7-bit LFSR)
        if (_widthMode)
        {
            // Clear bit 6 first, then set if needed
            _lfsr &= unchecked((ushort)~0x40);
            if (xorResult == 1)
                _lfsr |= 0x40;
        }
    }

    protected override void RefreshOutputState()
    {
        if (!IsChannelOn)
        {
            CurrentOutput = 0;
            return;
        }
        
        // Per docs: "The waveform output is bit 0 of the LFSR, INVERTED"
        int lfsrBit0 = _lfsr & 0x1;
        int invertedBit = lfsrBit0 == 0 ? 1 : 0;
        
        // Multiply by current volume envelope level
        CurrentOutput = invertedBit * VolumeLevel;
    }

    protected override void ResetLengthTimerValue()
    {
        LengthTimer = 64;
    }

    protected override void ResetPeriodTimer()
    {
        // Per docs: "The noise channel's frequency timer period is set by a base divisor shifted left some number of bits"
        // Period = divisor << clockShift
        int divisor = DivisorTable[_divisorCode];
        PeriodTimer = divisor << _clockShift;
    }

    /// <summary>
    /// NR43: Clock shift, Width mode of LFSR, Divisor code
    /// Format: SSSS WDDD
    /// </summary>
    public override void SetPeriodLowOrRandomnessRegister(ref byte value)
    {
        PeriodLowOrRandomness = value;
        
        _clockShift = (byte)((value & 0b1111_0000) >> 4);
        _widthMode = (value & 0b0000_1000) != 0;
        _divisorCode = (byte)(value & 0b0000_0111);
        
        ResetPeriodTimer();
    }

    public override void SetLengthTimer(ref byte value)
    {
        // NR41: Only lower 6 bits used for length (bits 5-0)
        InitialLengthTimer = (byte)(value & 0b0011_1111);
        LengthTimer = 64 - InitialLengthTimer;
    }

    protected override void Trigger()
    {        
        _lfsr = 0x7FFF; // All 15 bits set to 1
        
        base.Trigger();
    }

    public override void Reset()
    {
        base.Reset();
        _lfsr = 0x7FFF;
        _clockShift = 0;
        _widthMode = false;
        _divisorCode = 0;
    }
}