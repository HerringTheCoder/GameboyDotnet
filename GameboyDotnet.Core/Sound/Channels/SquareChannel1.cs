using GameboyDotnet.Extensions;
using GameboyDotnet.Sound.Channels.BuildingBlocks;

namespace GameboyDotnet.Sound.Channels;

public class SquareChannel1(AudioBuffer audioBuffer) : BaseSquareChannel()
{
    private int _sweepTimer;
    private bool _isSweepEnabled;
    private int _periodShadowRegister;
    
    private byte _currentPaceValue;
    private byte _requestedPaceValue;
    private bool _direction; //True = Addition, False = Subtraction
    private byte _individualStep;


    public override void Reset()
    {
        base.Reset();
        _sweepTimer = 0;
        _isSweepEnabled = false;
        _periodShadowRegister = 0;
        _currentPaceValue = 0;
        _requestedPaceValue = 0;
    }

    public void TickSweep()
    {
        if (!IsChannelOn)
            return;
        
        //TODO: Implement Sweep
    }

    public void SetSweepState(ref byte value)
    {
        _requestedPaceValue = (byte)((value & 0b0111_0000) >> 4);
        if (_requestedPaceValue == 0)
        {
            _isSweepEnabled = false;
            return;
        }

        _direction = value.IsBitSet(3);
        _individualStep = (byte)(value & 0b0000_0111);
    }

    protected override void Trigger()
    {
        if (!IsChannelOn)
        {
            _periodShadowRegister = PeriodTimer;
            _sweepTimer = _requestedPaceValue;
            _isSweepEnabled = true;
        }
        
        base.Trigger();
    }
}