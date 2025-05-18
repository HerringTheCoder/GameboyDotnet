namespace GameboyDotnet.Sound.Channels.BuildingBlocks;

public class BaseSquareChannel(AudioBuffer audioBuffer) : BaseChannel(audioBuffer)
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
        var isPeriodDividerCountdownFinished = StepPeriodDividerTimer();

        if (isPeriodDividerCountdownFinished)
        {
            DutyCycleStep = (DutyCycleStep + 1) & 0b111; //Wrap after 7
        }

        StepSampleState();
    }
    
    protected override void StepSampleState()
    {
        CurrentOutput = DutyCycles[WaveDutyIndex][DutyCycleStep] == 1 && IsChannelOn
            ? VolumeLevel   // Integer 0–15
            : 0;
    }

    public override void UpdateLengthDuty(ref byte value)
    {
        WaveDutyIndex = (value & 0b1100_0000) >> 6;
        base.UpdateLengthDuty(ref value);
    }

    protected override void Trigger()
    {
        if (!IsChannelOn)
            DutyCycleStep = 0;
        
        base.Trigger();
    }
}