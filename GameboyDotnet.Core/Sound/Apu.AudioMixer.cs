using GameboyDotnet.Sound.Channels;

namespace GameboyDotnet.Sound;

public partial class Apu
{
    private float _capacitorLeft = 0f;
    private float _capacitorRight = 0f;
    private (float leftPcmSample, float rightPcmSample) MixAudioChannelsToStereoSample()
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
        
        return (NormalizeToPcmSample(leftSum), NormalizeToPcmSample(rightSum));
    }

    private float NormalizeToPcmSample(float sample)
    {
        //Normalize digital [0-15]*4 channels to [0-2f], then shift to [-1f, 1f]
        return sample / MaxDigitalSumOfOutputPerStereoChannel * 2f - 1f;
    }
}