using GameboyDotnet.Extensions;
using GameboyDotnet.Sound.Channels;

namespace GameboyDotnet.Sound;

public partial class Apu
{
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
            _frameSequencerCyclesTimer = FrameSequencerCyclesPerFrame;
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