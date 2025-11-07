﻿namespace GameboyDotnet.Sound;

public partial class Apu
{
    //Frame sequencer is ticked at 512Hz, this should keep it steady between DMG and GBC modes
    private static readonly int FrameSequencerCyclesPerFrame = Cycles.CyclesPerSecond/512;
    internal int _frameSequencerCyclesTimer = FrameSequencerCyclesPerFrame;
    internal int _frameSequencerPosition = 0;
    
    private void StepFrameSequencer()
    {
        _frameSequencerCyclesTimer--;

        if (_frameSequencerCyclesTimer > 0)
            return;

        _frameSequencerCyclesTimer = FrameSequencerCyclesPerFrame;

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
}