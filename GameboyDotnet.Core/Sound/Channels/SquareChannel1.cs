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
    private EnvelopeDirection _frequencyEnvelopeDirection; //True = Addition, False = Subtraction
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
        if (!_isSweepEnabled)
            return;

        _sweepTimer--;
        if (_sweepTimer <= 0)
        {
            //https://gbdev.gg8.se/wiki/articles/Gameboy_sound_hardware - Obscure Behavior
            //The volume envelope and sweep timers treat a period of 0 as 8.
            int effectivePace = _currentPaceValue == 0 ? 8 : _currentPaceValue;
            _sweepTimer = effectivePace;

            if (_currentPaceValue != 0)
            {
                int newPeriodValue = CalculatePeriodAfterSweep();
                
                if (newPeriodValue <= 2047 && _individualStep != 0)
                {
                    _periodShadowRegister = newPeriodValue;
                    UpdatePeriodRegisters(newPeriodValue);
                    
                    //Check if next sweep will overflow the period value, then shutdown channel if true
                    int secondNewPeriod = CalculatePeriodAfterSweep();
                    if (secondNewPeriod > 2047)
                    {
                        IsChannelOn = false;
                    }
                }
            }
        }

        //TODO: Implement Sweep
    }

    private void UpdatePeriodRegisters(int newPeriodValue)
    {
        PeriodLowOrRandomness = (byte)(newPeriodValue & 0xFF);
        PeriodHigh = (byte)((PeriodHigh & 0xF8) | ((newPeriodValue >> 8) & 0x07));
        ResetPeriodTimer();
    }

    public void SetSweepState(ref byte value)
    {
        _requestedPaceValue = (byte)((value & 0b0111_0000) >> 4);
        if (_requestedPaceValue == 0)
        {
            _isSweepEnabled = false;
            return;
        }

        _frequencyEnvelopeDirection = value.IsBitSet(3) ? EnvelopeDirection.Descending : EnvelopeDirection.Ascending;
        _individualStep = (byte)(value & 0b0000_0111);
    }

    protected override void Trigger()
    {
        // Square 1's frequency is copied to shadow register
        _periodShadowRegister = GetPeriodValueFromRegisters;
    
        // Sweep timer is reloaded
        int effectivePace = _requestedPaceValue == 0 ? 8 : _requestedPaceValue;
        _sweepTimer = effectivePace;
        _currentPaceValue = _requestedPaceValue;
    
        // Internal enabled flag is set if either sweep period or shift are non-zero
        _isSweepEnabled = (_requestedPaceValue != 0 || _individualStep != 0);
    
        // If sweep shift is non-zero, perform calculation and overflow check immediately
        if (_individualStep != 0)
        {
            int newFrequency = CalculatePeriodAfterSweep();
            if (newFrequency > 2047)
            {
                IsChannelOn = false;
            }
        }
        
        base.Trigger();
    }

    private int CalculatePeriodAfterSweep()
    {
        int periodSweep = _periodShadowRegister >> _individualStep;

        if (_frequencyEnvelopeDirection is EnvelopeDirection.Ascending)
        {
            return _periodShadowRegister + periodSweep;
        }
        else
        {
            return _periodShadowRegister - periodSweep;
        }
    }
}