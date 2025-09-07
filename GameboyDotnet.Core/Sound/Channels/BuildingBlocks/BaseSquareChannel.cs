namespace GameboyDotnet.Sound.Channels.BuildingBlocks;

public abstract class BaseSquareChannel() : BaseChannel()
{
    protected byte[][] DutyCycles =
    [
        [0, 0, 0, 0, 0, 0, 0, 1], //12,5%
        [1, 0, 0, 0, 0, 0, 0, 1], //25%
        [1, 0, 0, 0, 0, 1, 1, 1], //50%
        [0, 1, 1, 1, 1, 1, 1, 0] //72,5%
    ];

    public int DutyCycleStep = 0;

    //NR11-NR21
    public int WaveDutyIndex = 0;

    public override void Step()
    {
        if (!IsChannelOn)
            return;

        var isPeriodTimerFinished = StepPeriodTimer();

        if (isPeriodTimerFinished)
        {
            DutyCycleStep = (DutyCycleStep + 1) & 0b111; //Wrap after 7
            RefreshOutputState();
        }
    }

    protected override bool StepPeriodTimer()
    {
        PeriodTimer--;
        if (PeriodTimer > 0)
        {
            return false;
        }
        
        ResetPeriodTimerPreserveLowerBits();

        return true;
    }

    public override void Reset()
    {
        base.Reset();
        DutyCycleStep = 0;
        WaveDutyIndex = 0;
    }

    protected override void RefreshOutputState()
    {
        if (!IsChannelOn)
            return;

        CurrentOutput = DutyCycles[WaveDutyIndex][DutyCycleStep] == 1 && IsChannelOn
            ? VolumeLevel // (Hi-state) 1 * [0–15]
            : 0;
    }

    public override void SetLengthTimer(ref byte value)
    {
        WaveDutyIndex = (value & 0b1100_0000) >> 6;
        InitialLengthTimer = value & 0b0011_1111;
        LengthTimer = 64 - InitialLengthTimer;
    }

    protected override void Trigger()
    {
        DutyCycleStep = 0;
        base.Trigger();
    }

    protected override void ResetLengthTimerValue()
    {
        LengthTimer = 64;
    }

    protected override void ResetPeriodTimer()
    {
        PeriodTimer = (2048 - GetPeriodValueFromRegisters) * 4;
    }

    public void ResetPeriodTimerPreserveLowerBits()
    {
        int lowerBitsOfPeriodDividerTimer = PeriodTimer & 0b11;
        PeriodTimer = ((2048 - GetPeriodValueFromRegisters) * 4 & ~0b11) | lowerBitsOfPeriodDividerTimer;
    }
}