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
    protected bool VolumeEnvelopeRunning = true; // Tracks if envelope should continue updating

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
        VolumeEnvelopeRunning = true;
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
                CurrentOutput = 0; // Ensure output is silenced when channel is disabled
            }
        }
    }

    public void TickVolumeEnvelopeTimer()
    {
        if (VolumeEnvelopePace == 0)
            return;
        
        // If envelope has stopped (reached limit of output = 15), don't continue updating
        if (!VolumeEnvelopeRunning)
            return;
        
        //https://gbdev.gg8.se/wiki/articles/Gameboy_sound_hardware - Obscure Behavior
        //The volume envelope and sweep timers treat a period of 0 as 8.
        int effectiveVolumeEnvelopePace = VolumeEnvelopePace == 0 ? 8 : VolumeEnvelopePace;
        
        VolumeEnvelopeTimer--;

        if (VolumeEnvelopeTimer <= 0)
        {
            VolumeEnvelopeTimer = effectiveVolumeEnvelopePace;
            
            if (VolumeEnvelopeDirection is EnvelopeDirection.Ascending)
            {
                if (VolumeLevel < 15)
                    VolumeLevel++;
                else
                    VolumeEnvelopeRunning = false; // Stop envelope updates
            }
            else if (VolumeEnvelopeDirection is EnvelopeDirection.Descending)
            {
                if (VolumeLevel > 0)
                    VolumeLevel--;
                else
                    VolumeEnvelopeRunning = false; // Stop envelope updates
            }
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
            CurrentOutput = 0;
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
        VolumeEnvelopeRunning = true;
    }

    protected abstract void ResetLengthTimerValue();
    
    protected virtual void ResetPeriodTimer()
    {
        PeriodTimer = (2048 - GetPeriodValueFromRegisters) * 4;
    }

    internal void DumpRegisters(ref Span<byte> memoryDump, ref int index)
    {
        //TODO: Some of these are exceeding byte, also check if all integers need to be ones.
        //Also, flags can be likely stored in a single byte
        memoryDump[index++] = (byte)(IsChannelOn ? 1 : 0);
        memoryDump[index++] = (byte)(IsRightSpeakerOn ? 1 : 0);
        memoryDump[index++] = (byte)(IsLeftSpeakerOn ? 1 : 0);
        (memoryDump[index++], memoryDump[index++]) = InitialLengthTimer.ToBytesPair();
        (memoryDump[index++], memoryDump[index++]) = VolumeEnvelopePace.ToBytesPair();
        memoryDump[index++] = (byte)(VolumeEnvelopeDirection == EnvelopeDirection.Ascending ? 1 : 0);
        memoryDump[index++] = (byte)InitialVolume;
        memoryDump[index++] = (byte)(IsDacEnabled ? 1 : 0);
        memoryDump[index++] = PeriodLowOrRandomness;
        memoryDump[index++] = PeriodHigh;
        memoryDump[index++] = (byte)(IsLengthEnabled ? 1 : 0);
        (memoryDump[index++], memoryDump[index++]) = LengthTimer.ToBytesPair();
        (memoryDump[index++], memoryDump[index++]) = PeriodTimer.ToBytesPair();
        (memoryDump[index++], memoryDump[index++]) = VolumeEnvelopeTimer.ToBytesPair();
        memoryDump[index++] = (byte)VolumeLevel;
        memoryDump[index++] = (byte)(VolumeEnvelopeRunning ? 1 : 0);
    }

    internal void LoadRegistersFromDump(ReadOnlySpan<byte> memoryDump, ref int index)
    {
        IsChannelOn = memoryDump[index++] == 1;
        IsRightSpeakerOn = memoryDump[index++] == 1;
        IsLeftSpeakerOn = memoryDump[index++] == 1;
        InitialLengthTimer = memoryDump.Slice(index, 2).ToInt();
        index += 2;
        VolumeEnvelopePace = memoryDump.Slice(index, 2).ToInt();
        index += 2;
        VolumeEnvelopeDirection = memoryDump[index++] == 1 ? EnvelopeDirection.Ascending : EnvelopeDirection.Descending;
        InitialVolume = memoryDump[index++];
        IsDacEnabled = memoryDump[index++] == 1;
        PeriodLowOrRandomness = memoryDump[index++];
        PeriodHigh = memoryDump[index++];
        IsLengthEnabled = memoryDump[index++] == 1;
        LengthTimer = memoryDump.Slice(index, 2).ToInt();
        index += 2;
        PeriodTimer = memoryDump.Slice(index, 2).ToInt();
        index += 2;
        VolumeEnvelopeTimer = memoryDump.Slice(index, 2).ToInt();
        index += 2;
        VolumeLevel = memoryDump[index++];
        VolumeEnvelopeRunning = memoryDump[index++] == 1;
    }
}
