using GameboyDotnet.Extensions;
using GameboyDotnet.Memory;
using GameboyDotnet.Sound.Channels;
using GameboyDotnet.Timers;

namespace GameboyDotnet.Sound;

public class Apu
{
    public AudioBuffer AudioBuffer { get; init; }
    public SquareChannel2 SquareChannel2 { get; init; }

    private const int ApuTStatesPerCpuCycle = 4;
    private byte _dividerRegister => _memoryController.IoRegisters.MemorySpaceView[0x04];
    private BitState _currentDividerRegisterBitState = BitState.Lo;
    private int _apuTCyclesCounter = 0;
    private int _divApuCounter = 0;

    private readonly MemoryController _memoryController;

    public Apu(MemoryController memoryController)
    {
        AudioBuffer = new AudioBuffer();
        _memoryController = memoryController;

        //SquareChannel1
        SquareChannel2 = new SquareChannel2(memoryController, AudioBuffer);
        //WaveChannel3
        //NoiseChannel4
    }

    public void PushApuCycles(ref byte tCycles)
    {
        _apuTCyclesCounter += tCycles / ApuTStatesPerCpuCycle;

        if (_apuTCyclesCounter > 2048)
        {
            _apuTCyclesCounter -= 2048;
        }

        var divApuTicked = DivFallingEdgeDetected();
        if (divApuTicked)
        {
            StepFrameSequencer(ref tCycles);
        }

        SquareChannel2.Step(ref tCycles);
    }

    public void ResetFrameSequencer()
    {
    }

    private void StepFrameSequencer(ref byte tCycles)
    {
        _divApuCounter = (_divApuCounter + 1) & 0b111; //Wrap to 7

        switch (_divApuCounter)
        {
            case 0:
                UpdateLengthCounters(ref tCycles);
                break;
            case 1:
                break;
            case 2:
                UpdateLengthCounters(ref tCycles);
                UpdateSweep();
                break;
            case 3:
                break;
            case 4:
                UpdateLengthCounters(ref tCycles);
                break;
            case 5:
                break;
            case 6:
                UpdateLengthCounters(ref tCycles);
                UpdateSweep();
                break;
            case 7:
                UpdateVolumeEnvelope(ref tCycles);
                break;
        }
    }

    private void UpdateVolumeEnvelope(ref byte tCycles)
    {
        SquareChannel2.UpdateVolume(ref tCycles);
    }

    private void UpdateSweep()
    {
        // throw new NotImplementedException();
    }

    private void UpdateLengthCounters(ref byte tCycles)
    {
        SquareChannel2.UpdateLengthTimer(ref tCycles);
    }

    private bool DivFallingEdgeDetected()
    {
        var previousDividerRegisterBitState = _currentDividerRegisterBitState;
        
        _currentDividerRegisterBitState = 
            _dividerRegister.IsBitSet(Cycles.DivFallingEdgeDetectorBitIndex)
                ? BitState.Hi
                : BitState.Lo;
        
        return previousDividerRegisterBitState == BitState.Hi && _currentDividerRegisterBitState == BitState.Lo;
    }
}