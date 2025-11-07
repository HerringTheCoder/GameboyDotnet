using GameboyDotnet.Sound.Channels;
using GameboyDotnet.Sound.Channels.BuildingBlocks;

namespace GameboyDotnet.Sound;

public partial class Apu
{
    public AudioBuffer AudioBuffer { get; init; }
    public SquareChannel1 SquareChannel1 { get; private set; }
    public SquareChannel2 SquareChannel2 { get; private set; }
    public WaveChannel WaveChannel { get; private set; }
    public NoiseChannel NoiseChannel { get; private set; }
    public BaseChannel[] AvailableChannels { get; private set; }
    public bool IsAudioOn { get; internal set; }
    public byte LeftMasterVolume { get; internal set; }
    public byte RightMasterVolume { get; internal set; }
    
    private const int MaxDigitalSumOfOutputPerStereoChannel = 15 * 4 * 7; //4 channels, 0-15 volume level each, 0-7 Left/Right Master volume level

    public Apu()
    {
        AudioBuffer = new AudioBuffer();
        SquareChannel1 = new SquareChannel1(AudioBuffer);
        SquareChannel2 = new SquareChannel2();
        WaveChannel = new WaveChannel(AudioBuffer);
        NoiseChannel = new NoiseChannel();
        AvailableChannels = [SquareChannel1, SquareChannel2, WaveChannel, NoiseChannel];
    }

    internal int SampleCounter = 87;

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
                var (leftSample, rightSample) = MixAudioChannelsToStereoSamples();
                AudioBuffer.EnqueueSample(leftSample, rightSample);
            }
        }
    }
}