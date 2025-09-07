using GameboyDotnet.Sound.Channels;
using GameboyDotnet.Sound.Channels.BuildingBlocks;

namespace GameboyDotnet.Sound;

public partial class Apu
{
    private (float leftPcmSample, float rightPcmSample) MixAudioChannelsToStereoSamples()
    {
        int leftSum = 0;
        int rightSum = 0;
        bool isAnyDacEnabled = false;

        foreach (var channel in AvailableChannels)
        {
            if (channel.IsDacEnabled)
                isAnyDacEnabled = true;

            if (channel is { IsChannelOn: true, IsDebugEnabled: true })
            {
                if (channel.IsLeftSpeakerOn) leftSum += channel.CurrentOutput;
                if (channel.IsRightSpeakerOn) rightSum += channel.CurrentOutput;
            }
        }

        //Apply master volume panning
        leftSum *= LeftMasterVolume;
        rightSum *= RightMasterVolume;
        var (leftSample, rightSample) = (NormalizeToPcmSample(leftSum), NormalizeToPcmSample(rightSum));
        FrequencyFilters.ApplyHighPassFilter(ref leftSample, ref rightSample, isAnyDacEnabled);
        
        return (NormalizeToPcmSample(leftSum), NormalizeToPcmSample(rightSum));
    }

    private float NormalizeToPcmSample(float sample)
    {
        //Normalize digital [0-15]*4 channels to [0-2f], then shift to [-1f, 1f]
        return sample / MaxDigitalSumOfOutputPerStereoChannel * 2f - 1f;
    }
}