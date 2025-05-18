using GameboyDotnet.Extensions;
using GameboyDotnet.Memory;
using GameboyDotnet.Sound.Channels;
using GameboyDotnet.Timers;

namespace GameboyDotnet.Sound;

public class Apu
{
    public AudioBuffer AudioBuffer { get; init; }
    public SquareChannel1 SquareChannel1 { get; private set; }
    public SquareChannel2 SquareChannel2 { get; private set; }
    public bool IsAudioOn { get; private set; }
    public byte LeftMasterVolume { get; private set; }
    public byte RightMasterVolume { get; private set; }


    private BitState _currentDividerRegisterBitState = BitState.Lo;
    private int _frameSequencerCyclesTimer = 8192;
    private int _frameSequencerPosition = 0;
    private int _maxDigitalSumOfOutputPerChannel = 15 * 4; //4 channels, 0-15 volume level each

    public Apu()
    {
        AudioBuffer = new AudioBuffer();
        SquareChannel1 = new SquareChannel1(AudioBuffer);
        SquareChannel2 = new SquareChannel2(AudioBuffer);
    }

    private int SampleCounter = 87;

    public void PushApuCycles(ref byte tCycles)
    {
        for (int i = tCycles; i > 0; i--)
        {
            StepFrameSequencer();
            SquareChannel1.Step();
            SquareChannel2.Step();
            //WaveChannel.Step();
            //NoiseChannel.Step();

            SampleCounter--;
            if(SampleCounter > 0)
                continue;
            
            SampleCounter = 87; 
            if (IsAudioOn)
            {
                int leftSum = 0;
                int rightSum = 0;

                if (SquareChannel1.IsLeftSpeakerOn) leftSum += SquareChannel1.CurrentOutput;
                if (SquareChannel1.IsRightSpeakerOn) rightSum += SquareChannel1.CurrentOutput;
                if (SquareChannel2.IsLeftSpeakerOn) leftSum += SquareChannel2.CurrentOutput;
                if (SquareChannel2.IsRightSpeakerOn) rightSum += SquareChannel2.CurrentOutput;
                // if(WaveChannel.IsLeftSpeakerOn) leftSample += WaveChannel.CurrentSample;
                // if(WaveChannel.IsRightSpeakerOn) rightSample += WaveChannel.CurrentSample;
                // if(NoiseChannel.IsLeftSpeakerOn) leftSample += NoiseChannel.CurrentSample;
                // if(NoiseChannel.IsRightSpeakerOn) rightSample += NoiseChannel.CurrentSample;
                
                //Normalize digital [0-15]*4 channels to [0-2f], then shift to [-1f, 1f]
                float normalizedLeft = (float)leftSum / _maxDigitalSumOfOutputPerChannel *2f - 1f; 
                float normalizedRight = (float)rightSum / _maxDigitalSumOfOutputPerChannel *2f - 1f;
                AudioBuffer.EnqueueSample(leftSum, rightSum);
            }
        }
    }

    public void ResetFrameSequencer()
    {
    }

    private void StepFrameSequencer()
    {
        _frameSequencerCyclesTimer--;

        if (_frameSequencerCyclesTimer > 0)
            return;

        _frameSequencerCyclesTimer = 8192;

        _frameSequencerPosition = (_frameSequencerPosition + 1) & 0b111; //Wrap to 7

        switch (_frameSequencerPosition)
        {
            case 0:
                TickLengthCounters();
                break;
            case 1:
                break;
            case 2:
                TickLengthCounters();
                TickSweep();
                break;
            case 3:
                break;
            case 4:
                TickLengthCounters();
                break;
            case 5:
                break;
            case 6:
                TickLengthCounters();
                TickSweep();
                break;
            case 7:
                TickVolumeEnvelope();
                break;
        }
    }

    private void TickVolumeEnvelope()
    {
        SquareChannel2.UpdateVolume();
    }

    private void TickSweep()
    {
        // throw new NotImplementedException();
    }

    private void TickLengthCounters()
    {
        SquareChannel2.TickLengthTimer();
    }

    private bool DivFallingEdgeDetected(ref byte dividerValue)
    {
        var previousDividerRegisterBitState = _currentDividerRegisterBitState;

        _currentDividerRegisterBitState =
            dividerValue.IsBitSet(Cycles.DivFallingEdgeDetectorBitIndex)
                ? BitState.Hi
                : BitState.Lo;

        return previousDividerRegisterBitState == BitState.Hi && _currentDividerRegisterBitState == BitState.Lo;
    }

    public void UpdatePowerState(ref byte value)
    {
        if (IsAudioOn && !value.IsBitSet(7))
        {
            IsAudioOn = false;
        }
        else if (!IsAudioOn && value.IsBitSet(7))
        {
            IsAudioOn = true;
        }
    }

    public void UpdateChannelPanningStates(ref byte value)
    {
        //TODO: Implement other channels
        SquareChannel2.IsLeftSpeakerOn = value.IsBitSet(4);
        SquareChannel2.IsRightSpeakerOn = value.IsBitSet(0);
        SquareChannel2.IsLeftSpeakerOn = value.IsBitSet(5);
        SquareChannel2.IsRightSpeakerOn = value.IsBitSet(1);
    }

    public void UpdateVolumeControlStates(ref byte value)
    {
        //Ignores VIN input, bits 7 and 3
        //Value of 0 means 'very quiet', 7 means full volume
        LeftMasterVolume = (byte)((value & 0b0111_0000) >> 4);
        RightMasterVolume = (byte)(value & 0b0000_0111);
    }
}