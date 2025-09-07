using GameboyDotnet.Extensions;

namespace GameboyDotnet.Sound.Channels.BuildingBlocks;

public abstract class BaseChannel()
{
    //Debug
    public bool IsDebugEnabled = true;
    
    //Shadow registers
    public bool IsChannelOn;
    public bool IsRightSpeakerOn;
    public bool IsLeftSpeakerOn;

    //NRx1
    public int InitialLengthTimer;

    //NRX2
    public int VolumeEnvelopePace; //also referred to as: Volume Envelop Period
    public EnvelopeDirection VolumeEnvelopeDirection;
    public int InitialVolume;
    public bool IsDacEnabled;

    //NRX3
    public byte PeriodLowOrRandomness;

    //NRX4
    public byte PeriodHigh;
    public bool IsLengthEnabled; // Bit 6

    public int GetPeriodValueFromRegisters => ((PeriodHigh & 0b111) << 8) | PeriodLowOrRandomness;

    public int LengthTimer;
    public int PeriodTimer = 0;
    public int VolumeEnvelopeTimer = 0;
    protected int VolumeLevel;


    public int CurrentOutput;

    public abstract void Step();

    protected abstract void RefreshOutputState();

    public virtual void Reset()
    {
        IsChannelOn = false;
        IsRightSpeakerOn = false;
        IsLeftSpeakerOn = false;
        InitialLengthTimer = 0;
        VolumeEnvelopePace = 0;
        VolumeEnvelopeDirection = EnvelopeDirection.Descending;
        InitialVolume = 0;
        IsDacEnabled = false;
        PeriodLowOrRandomness = 0;
        PeriodHigh = 0;
        IsLengthEnabled = false;
        LengthTimer = 0;
        PeriodTimer = 0;
        VolumeEnvelopeTimer = 0;
        VolumeLevel = 0;
    }

    protected virtual bool StepPeriodTimer()
    {
        PeriodTimer--;
        if (PeriodTimer > 0)
        {
            return false;
        }

        ResetPeriodTimer();
        return true;
    }
    
    public void StepLengthTimer()
    {
        if (!IsChannelOn)
            return;
        
        //Check if LengthTimer is enabled
        if (IsLengthEnabled && LengthTimer > 0)
        {
            LengthTimer--;
            if (LengthTimer == 0)
            {
                IsChannelOn = false;
            }
        }
    }

    public void TickVolumeEnvelopeTimer()
    {
        //https://gbdev.gg8.se/wiki/articles/Gameboy_sound_hardware - Obscure Behavior
        //The volume envelope and sweep timers treat a period of 0 as 8.
        int effectiveVolumeEnvelopePace = VolumeEnvelopePace == 0 ? 8 : VolumeEnvelopePace;
        
        VolumeEnvelopeTimer--;

        if (VolumeEnvelopeTimer <= 0)
        {
            VolumeEnvelopeTimer = effectiveVolumeEnvelopePace;
            
            if (VolumeEnvelopeDirection is EnvelopeDirection.Ascending && VolumeLevel < 15)
                VolumeLevel++;
            else if (VolumeEnvelopeDirection is EnvelopeDirection.Descending && VolumeLevel > 0)
                VolumeLevel--;
        }
    }

    public abstract void SetLengthTimer(ref byte value);

    public virtual void SetVolumeRegister(ref byte value)
    {
        InitialVolume = (value & 0b1111_0000) >> 4;
        VolumeEnvelopeDirection = value.IsBitSet(3) ? EnvelopeDirection.Ascending : EnvelopeDirection.Descending;
        VolumeEnvelopePace = value & 0b111;

        // DAC is enabled if any of the upper 5 bits are set (0xF8 mask)
        IsDacEnabled = (value & 0b1111_1000) != 0;
        if (!IsDacEnabled)
        {
            IsChannelOn = false;
        }
    }

    public virtual void SetPeriodLowOrRandomnessRegister(ref byte value)
    {
        PeriodLowOrRandomness = value;
    }

    public void SetPeriodHighControl(ref byte value)
    {
        bool oldLengthEnabled = IsLengthEnabled;
        IsLengthEnabled = value.IsBitSet(6);
        PeriodHigh = (byte)(value & 0b111);

        //TODO: if(!oldLengthEnabled && IsLengthEnabled && FrameSequencerWillNotClockLengthThisSteps && LengthTimer >0)
        // {
        //     LengthTimer--;
        //     if (LengthTimer == 0)
        //     {
        //         IsChannelOn = false;
        //     }
        // }
        
        if (value.IsBitSet(7))
        {
            Trigger();
        }
    }

    protected virtual void Trigger()
    {
        IsChannelOn = IsDacEnabled;

        if (LengthTimer == 0)
        {
            ResetLengthTimerValue();
            // TODO: if (IsLengthEnabled && FrameSequencerWillNotClockLengthThisStep())
            // {
            //     LengthTimer--;
            // }
        }
        
        ResetPeriodTimer();
        int effectiveVolumeEnvelopePace = VolumeEnvelopePace == 0 ? 8 : VolumeEnvelopePace;
        VolumeEnvelopeTimer = effectiveVolumeEnvelopePace;
        VolumeLevel = InitialVolume;
    }

    protected abstract void ResetLengthTimerValue();
    
    protected virtual void ResetPeriodTimer()
    {
        PeriodTimer = (2048 - GetPeriodValueFromRegisters) * 4;
    }
}
