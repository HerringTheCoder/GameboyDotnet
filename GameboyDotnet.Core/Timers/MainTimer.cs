﻿using GameboyDotnet.Components;

namespace GameboyDotnet.Timers;

public class MainTimer
{
    private int _tStatesCounter;
    
    internal void CheckAndIncrementTimer(ref byte durationTStates, MemoryController memoryController)
    {
        var timerControl = memoryController.ReadByte(Constants.TACRegister);
        if ((timerControl & 0b100) == 0) //Timer is disabled, do nothing
            return;
        
        _tStatesCounter += durationTStates;
        var timerTCycles = GetTimerTCycles(timerControl);

        if (_tStatesCounter >= timerTCycles)
        {
            _tStatesCounter -= timerTCycles; //Reset the counter, but keep the remainder
            memoryController.IncrementByte(Constants.TIMARegister);
            var timerValue = memoryController.ReadByte(Constants.TIMARegister);

            if (timerValue == 0) //Overflow, TIMA = TMA, then request an interrupt
            {
                memoryController.WriteByte(Constants.TIMARegister, memoryController.ReadByte(Constants.TMARegister));
                memoryController.WriteByte(
                    Constants.IFRegister,
                    (byte)(memoryController.ReadByte(Constants.IFRegister) | 0b100)
                );
            }
        }
    }

    private static int GetTimerTCycles(int timerControl)
    {
        return (timerControl & 0b11) switch
        {
            0b00 => 1024,
            0b01 => 16,
            0b10 => 64,
            0b11 => 256,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}