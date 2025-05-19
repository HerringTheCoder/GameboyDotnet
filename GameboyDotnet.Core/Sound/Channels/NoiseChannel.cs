using GameboyDotnet.Sound.Channels.BuildingBlocks;

namespace GameboyDotnet.Sound.Channels;

public class NoiseChannel() : BaseChannel()
{
    public override void Step()
    {
        if (!IsChannelOn)
            return;

        RefreshOutputState();
    }

    protected override void RefreshOutputState()
    {
        
    }

    protected override void ResetLengthTimerValue()
    {
        LengthTimer = 64;
    }

    /// <summary>
    /// Period low bits values are used for frequency and randomness
    /// </summary>
    /// <param name="value"></param>
    public override void SetPeriodLowOrRandomnessRegister(ref byte value)
    {
        PeriodLowOrRandomness = value;
    }
}