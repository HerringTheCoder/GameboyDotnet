using GameboyDotnet.Extensions;
using GameboyDotnet.Sound.Channels;

namespace GameboyDotnet.Sound;

public partial class Apu
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
}