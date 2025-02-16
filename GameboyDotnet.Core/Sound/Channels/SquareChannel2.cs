using GameboyDotnet.Extensions;
using GameboyDotnet.Memory;

namespace GameboyDotnet.Sound.Channels;

public class SquareChannel2(MemoryController memoryController, AudioBuffer audioBuffer)
{
    public byte Nr21Channel2LengthTimerAndDutyCycle => memoryController.IoRegisters.MemorySpaceView[0x21];
    
    public byte Nr22Channel2VolumeAndEnvelope => memoryController.IoRegisters.MemorySpaceView[0x22];

    public byte Nr23Channel2PeriodLow => memoryController.IoRegisters.MemorySpaceView[0x23];
    
    public byte Nr24Channel2PeriodHighAndControl => memoryController.IoRegisters.MemorySpaceView[0x24];
    
    private byte[][] DutyCycles =
    [
        [0, 0, 0, 0, 0, 0, 0, 1], //12,5%
        [0, 0, 0, 0, 0, 0, 1, 1], //25%
        [0, 0, 0, 0, 1, 1, 1, 1], //50%
        [1, 1, 1, 1, 1, 1, 0, 0]  //72,5%
    ];
    
    /// <summary>
    /// Increments at 256Hz frequency, same cycle as DIV-APU, when it reaches 64, the channel is turned off
    /// </summary>
    public int LengthTimer;
    
    public int PeriodDividerTimer = 0;
    public int GetPeriodValueFromRegisters => ((Nr24Channel2PeriodHighAndControl & 0b111) << 8) | Nr23Channel2PeriodLow;
    public int VolumeEnvelopeTimer = 0;
    
    private int DutyCycleStep = 0;
    private int VolumeLevel;
    
    private int SampleRateCounter = 87; //Gather samples each 87 cycles, so we'll get about 44,1Khz
    private BitState _triggerBit = BitState.Lo;

    public void Step(ref byte tCycles)
    {
        var isPeriodDividerCountdownFinished = UpdatePeriodDividerTimer(ref tCycles);
        
        if (isPeriodDividerCountdownFinished)
        {
            DutyCycleStep = (DutyCycleStep + 1) & 0b111; //Wrap after 7
        }

        UpdateSampleState(ref tCycles);
    }

    private void UpdateSampleState(ref byte tCycles)
    {
        SampleRateCounter -= tCycles;
        if (SampleRateCounter <= 0)
        {
            SampleRateCounter += 87; //TODO: Read from audioBuffer.SampleRate and determine the number

            //Normalize 0-15 from Gameboy to 0.0f-1.0f range
            var normalizedVolumeFactor = Math.Clamp((float)VolumeLevel / 15, 0.0f, 1.0f);
            
            //Value from 0 to 3
            var dutyCyclesIndex = (Nr21Channel2LengthTimerAndDutyCycle & 0b11_00_00_00) >> 6;
           
            var sample = DutyCycles[dutyCyclesIndex][DutyCycleStep] == 1
                ? 1.0f * normalizedVolumeFactor
                : -1.0f * normalizedVolumeFactor; //TODO: Should low value be also multiplied?
            
            Console.WriteLine($"Sample: {sample} (Volume: {VolumeLevel}");
            
            audioBuffer.EnqueueSample(sample);
        }
    }

    private bool UpdatePeriodDividerTimer(ref byte tCycles)
    {
        PeriodDividerTimer -= tCycles;
        if (PeriodDividerTimer > 0)
        {
            return false;
        }
        
        //TODO: Double check the math on resetting PeriodTimer's value
        PeriodDividerTimer += (2048 - GetPeriodValueFromRegisters) * 4;
        
        return true;
    }

    public void UpdateLengthTimer(ref byte tCycles)
    {
        //Check if LengthTimer is enabled
        if(Nr24Channel2PeriodHighAndControl.IsBitSet(6))
        {
            LengthTimer -= tCycles;
            if (LengthTimer <= 0)
            {
                //Reset the timer to initial value from register
                LengthTimer += Nr21Channel2LengthTimerAndDutyCycle & 0b111111;
            }
        }
    }

    public void UpdateVolume(ref byte tCycles)
    {
        VolumeEnvelopeTimer -= tCycles;
        VolumeLevel = 2;

        if (VolumeEnvelopeTimer <= 0)
        {
            //4194304Hz / 64Hz = 65536 tCycles per update
            VolumeEnvelopeTimer += 65536;
            
            var volumeAndEnvelopeRegister = Nr22Channel2VolumeAndEnvelope;
            
            var volumeSweepPace = volumeAndEnvelopeRegister & 0b111;
            
            //VolumeSweepPace = 0 means the volume sweep is disabled
            if (volumeSweepPace == 0)
                return;
            
            var isEnvelopeDirectionRising = volumeAndEnvelopeRegister.IsBitSet(3);
            
            VolumeLevel = Math.Clamp(
                VolumeLevel += isEnvelopeDirectionRising
                ? volumeSweepPace
                : -volumeSweepPace,
                    0, 15);
                
            Console.WriteLine($"Updated Volume: {VolumeLevel}");
        }
    }
}