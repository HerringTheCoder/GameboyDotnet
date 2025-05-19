using GameboyDotnet.Extensions;
using GameboyDotnet.Sound.Channels;

namespace GameboyDotnet.Sound;

public class Apu
{
    public AudioBuffer AudioBuffer { get; init; }
    public SquareChannel1 SquareChannel1 { get; private set; }
    public SquareChannel2 SquareChannel2 { get; private set; }
    public WaveChannel WaveChannel { get; private set; }
    public NoiseChannel NoiseChannel { get; private set; }
    public bool IsAudioOn { get; private set; }
    public byte LeftMasterVolume { get; private set; }
    public byte RightMasterVolume { get; private set; }
    
    private int _frameSequencerCyclesTimer = 8192;
    private int _frameSequencerPosition = 0;
    private const int MaxDigitalSumOfOutputPerStereoChannel = 15 * 4 * 7; //4 channels, 0-15 volume level each, 0-7 Left/Right Master volume level

    public Apu()
    {
        AudioBuffer = new AudioBuffer();
        SquareChannel1 = new SquareChannel1(AudioBuffer);
        SquareChannel2 = new SquareChannel2();
        WaveChannel = new WaveChannel(AudioBuffer);
        NoiseChannel = new NoiseChannel();
    }

    private int SampleCounter = 87;

    public void PushApuCycles(ref byte tCycles)
    {
        for (int i = tCycles; i > 0; i--)
        {
            StepFrameSequencer();
            SquareChannel1.Step();
            SquareChannel2.Step();
            WaveChannel.Step();
            NoiseChannel.Step();

            SampleCounter--;
            if (SampleCounter > 0)
                continue;

            SampleCounter = 87;
            if (IsAudioOn)
            {
                MixAndPushSamples();
            }
        }
    }

    private void MixAndPushSamples()
    {
        int leftSum = 0;
        int rightSum = 0;

        if (SquareChannel1 is { IsDacEnabled: true, IsChannelOn: true, IsDebugEnabled: true })
        {
            if (SquareChannel1.IsLeftSpeakerOn) leftSum += SquareChannel1.CurrentOutput;
            if (SquareChannel1.IsRightSpeakerOn) rightSum += SquareChannel1.CurrentOutput;
        }
              
        if(SquareChannel2 is { IsDacEnabled: true, IsChannelOn: true, IsDebugEnabled: true })
        {
            if (SquareChannel2.IsLeftSpeakerOn) leftSum += SquareChannel2.CurrentOutput;
            if (SquareChannel2.IsRightSpeakerOn) rightSum += SquareChannel2.CurrentOutput;
        }
        
        if(WaveChannel is { IsDacEnabled: true, IsChannelOn: true, IsDebugEnabled: true })
        {
            if (WaveChannel.IsLeftSpeakerOn) leftSum += WaveChannel.CurrentOutput;
            if (WaveChannel.IsRightSpeakerOn) rightSum += WaveChannel.CurrentOutput;
        }
        
        if(NoiseChannel is { IsDacEnabled: true, IsChannelOn: true, IsDebugEnabled: true })
        {
            if (NoiseChannel.IsLeftSpeakerOn) leftSum += NoiseChannel.CurrentOutput;
            if (NoiseChannel.IsRightSpeakerOn) rightSum += NoiseChannel.CurrentOutput;
        }

        //Apply master volume panning
        leftSum *= LeftMasterVolume;
        rightSum *= RightMasterVolume;
        //Normalize digital [0-15]*4 channels to [0-2f], then shift to [-1f, 1f]
        float normalizedLeft = (float)leftSum / MaxDigitalSumOfOutputPerStereoChannel * 2f - 1f;
        float normalizedRight = (float)rightSum / MaxDigitalSumOfOutputPerStereoChannel * 2f - 1f;
        AudioBuffer.EnqueueSample(normalizedLeft, normalizedRight);
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
        SquareChannel1.TickVolumeEnvelopeTimer();
        SquareChannel2.TickVolumeEnvelopeTimer();
        WaveChannel.TickVolumeEnvelopeTimer();
        NoiseChannel.TickVolumeEnvelopeTimer();
    }

    private void TickSweep()
    {
        SquareChannel1.TickSweep();
    }

    private void TickLengthCounters()
    {
        SquareChannel1.StepLengthTimer();
        SquareChannel2.StepLengthTimer();
        WaveChannel.StepLengthTimer();
        NoiseChannel.StepLengthTimer();
    }

    public void SetPowerState(ref byte value)
    {
        if (IsAudioOn && !value.IsBitSet(7))
        {
            IsAudioOn = false;
            LeftMasterVolume = 0;
            RightMasterVolume = 0;
            
            SquareChannel1.Reset();
            SquareChannel2.Reset();
            WaveChannel.Reset();
            NoiseChannel.Reset();
        }
        else if (!IsAudioOn && value.IsBitSet(7))
        {
            IsAudioOn = true;
            _frameSequencerCyclesTimer = 8192;
            _frameSequencerPosition = 0;
        }
    }

    public void SetChannelPanningStates(ref byte value)
    {
        SquareChannel1.IsLeftSpeakerOn = value.IsBitSet(4);
        SquareChannel1.IsRightSpeakerOn = value.IsBitSet(0);
        
        SquareChannel2.IsLeftSpeakerOn = value.IsBitSet(5);
        SquareChannel2.IsRightSpeakerOn = value.IsBitSet(1);
        
        WaveChannel.IsLeftSpeakerOn = value.IsBitSet(6);
        WaveChannel.IsRightSpeakerOn = value.IsBitSet(2);
        
        NoiseChannel.IsLeftSpeakerOn = value.IsBitSet(7);
        NoiseChannel.IsRightSpeakerOn = value.IsBitSet(3);
    }

    public void SetVolumeControlStates(ref byte value)
    {
        //Ignores VIN input, bits 7 and 3
        //Value of 0 means 'very quiet', 7 means full volume
        LeftMasterVolume = (byte)((value & 0b0111_0000) >> 4);
        RightMasterVolume = (byte)(value & 0b0000_0111);
    }
}