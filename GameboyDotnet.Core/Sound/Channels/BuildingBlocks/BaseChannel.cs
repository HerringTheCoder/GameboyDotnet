using GameboyDotnet.Extensions;

namespace GameboyDotnet.Sound.Channels.BuildingBlocks;

public abstract class BaseChannel(AudioBuffer audioBuffer)
{
    public bool IsChannelOn;
    public bool IsRightSpeakerOn;
    public bool IsLeftSpeakerOn;

    //NRx1
    public int InitialLengthTimer;

    //NRX2
    public int VolumeEnvelopeSweepPace;
    public bool VolumeEnvelopeDirection;
    public int InitialVolume;

    //NRX3
    public byte PeriodLow;

    //NRX4
    public byte PeriodHigh;
    public bool IsLengthEnabled; // Bit 6

    public int GetPeriodValueFromRegisters => ((PeriodHigh & 0b111) << 8) | PeriodLow;

    public int LengthTimer;
    public int PeriodDividerTimer = 0;
    public int VolumeEnvelopeTimer = 0;
    protected int VolumeLevel;


    public int CurrentOutput;

    public abstract void Step();

    protected abstract void StepSampleState();

    protected virtual bool StepPeriodDividerTimer()
    {
        PeriodDividerTimer--;
        if (PeriodDividerTimer > 0)
        {
            return false;
        }

        //TODO: Double check the math on resetting PeriodTimer's value
        PeriodDividerTimer += (2048 - GetPeriodValueFromRegisters) * 4;

        return true;
    }

    

    public void TickLengthTimer()
    {
        //Check if LengthTimer is enabled
        if (IsLengthEnabled)
        {
            LengthTimer--;
            if (LengthTimer <= 0)
            {
                IsChannelOn = false;
            }
        }
    }

    public void UpdateVolume()
    {
        int period = (VolumeEnvelopeSweepPace == 0) ? 8 : VolumeEnvelopeSweepPace;

        VolumeEnvelopeTimer--;

        if (VolumeEnvelopeTimer <= 0)
        {
            VolumeEnvelopeTimer = period;

            if (VolumeEnvelopeDirection && VolumeLevel < 15)
                VolumeLevel++;
            else if (!VolumeEnvelopeDirection && VolumeLevel > 0)
                VolumeLevel--;
        }
    }

    public virtual void UpdateLengthDuty(ref byte value)
    {
        InitialLengthTimer = value & 0b0011_1111;
        LengthTimer = 64 - InitialLengthTimer;
    }

    public void UpdateVolumeEnvelope(ref byte value)
    {
        InitialVolume = value & 0b1111_0000 >> 4;
        VolumeEnvelopeDirection = value.IsBitSet(3);
        VolumeEnvelopeSweepPace = value & 0b111;
    }

    public void UpdatePeriodLow(ref byte value)
    {
        PeriodLow = value;
    }

    public void UpdatePeriodHighControl(ref byte value)
    {
        IsLengthEnabled = value.IsBitSet(6);
        PeriodHigh = value;

        if (value.IsBitSet(7))
        {
            Trigger();
        }
    }

    protected virtual void Trigger()
    {
        IsChannelOn = true;

        if (LengthTimer == 0)
            LengthTimer = 64;

        PeriodDividerTimer = (2048 - GetPeriodValueFromRegisters) * 4;
        VolumeEnvelopeTimer = (VolumeEnvelopeSweepPace == 0) ? 8 : VolumeEnvelopeSweepPace;
        VolumeLevel = InitialVolume;
    }
}
