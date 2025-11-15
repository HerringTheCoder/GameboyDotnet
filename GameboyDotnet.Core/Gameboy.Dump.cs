using System.Buffers;
using GameboyDotnet.Extensions;
using GameboyDotnet.Memory.BuildingBlocks;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet;

public partial class Gameboy
{
    /// <summary>
    /// Dumps memory into provided span. Returns actual number of bytes stored.
    /// </summary>
    /// <param name="memoryDump"></param>
    /// <param name="totalExternalRamSize"></param>
    /// <returns></returns>
    public int DumpWritableMemory(Span<byte> memoryDump, int totalExternalRamSize)
    {
        for (var i = 0; i < memoryDump.Length; i++)
        {
            memoryDump[i] = 0;
        }
        
        List<FixedBank> fixedBanks =
        [
            Cpu.MemoryController.Vram,
            Cpu.MemoryController.Wram0,
            Cpu.MemoryController.Wram1,
            Cpu.MemoryController.IoRegisters,
            Cpu.MemoryController.Oam,
            Cpu.MemoryController.HRam
        ];

        var index = 0;
        foreach (var fixedBank in fixedBanks)
        {
            fixedBank.MemorySpaceView.CopyTo(memoryDump.Slice(index, fixedBank.MemorySpaceView.Length));
            index += fixedBank.MemorySpaceView.Length;
        }

        (memoryDump[index++], memoryDump[index++]) = Cpu.Register.PC.ToBytesPair();
        (memoryDump[index++], memoryDump[index++]) = Cpu.Register.SP.ToBytesPair();
        memoryDump[index++] = Cpu.Register.A;
        memoryDump[index++] = Cpu.Register.B;
        memoryDump[index++] = Cpu.Register.C;
        memoryDump[index++] = Cpu.Register.D;
        memoryDump[index++] = Cpu.Register.E;
        memoryDump[index++] = Cpu.Register.H;
        memoryDump[index++] = Cpu.Register.L;
        memoryDump[index++] = Cpu.Register.F;
        
        //Timers
        (memoryDump[index++], memoryDump[index++]) = TimaTimer.TStatesCounter.ToBytesPair();
        (memoryDump[index++], memoryDump[index++]) = DivTimer.DividerCycleCounter.ToBytesPair();
        
        //PPU
        memoryDump[index++] = Ppu.LyInternal;
        memoryDump[index++] = Ppu._windowLineCounter;
        (memoryDump[index++], memoryDump[index++]) = Ppu._cyclesCounter.ToBytesPair();
        memoryDump[index++] = (byte)(Ppu._windowYConditionTriggered ? 1 : 0);
        
        //APU
        memoryDump[index++] = Apu.LeftMasterVolume;
        memoryDump[index++] = Apu.RightMasterVolume;
        memoryDump[index++] = (byte)(Apu.IsAudioOn ? 1 : 0);
        (memoryDump[index++], memoryDump[index++]) = Apu.SampleCounter.ToBytesPair();
        (memoryDump[index++], memoryDump[index++]) = Apu._frameSequencerCyclesTimer.ToBytesPair();
        (memoryDump[index++], memoryDump[index++]) = Apu._frameSequencerPosition.ToBytesPair();
        foreach (var channel in Apu.AvailableChannels)
        {
            channel.DumpRegisters(ref memoryDump, ref index);
        }
        
        memoryDump[index++] = MemoryController.IoRegisters._joypadRegister;
        
        //CPU State
        memoryDump[index++] = (byte)(Cpu.IsHalted ? 1 : 0);
        memoryDump[index++] = (byte)(Cpu.Register.InterruptsMasterEnabled ? 1 : 0);
        memoryDump[index++] = (byte)(Cpu.Register.IMEPending ? 1 : 0);
        
        var mbc = MemoryController.RomBankNn;
        if (mbc is MemoryBankController mbcN)
        {
            memoryDump[index++] = (byte)mbcN.CurrentBank; //TODO: Can amount of banks exceed byte size?
            memoryDump[index++] = (byte)(mbcN.ExternalRamAndRtcEnabled ? 1 : 0);
            memoryDump[index++] = (byte)mbcN.RomBankingMode;
            mbcN.ExternalRam.MemorySpaceView.CopyTo(memoryDump.Slice(index, totalExternalRamSize));
            index += totalExternalRamSize;
        }
        
        _logger.LogWarning("Memory dump created. Stored {Index} actual bytes", index);

        return index + 1;
    }

    /// <summary>
    /// Loads memory dump from provided byte array.
    /// </summary>
    /// <param name="memoryDump"></param>
    public void LoadMemoryDump(byte[] memoryDump)
    {
        List<FixedBank> fixedBanks =
        [
            Cpu.MemoryController.Vram,
            Cpu.MemoryController.Wram0,
            Cpu.MemoryController.Wram1,
            Cpu.MemoryController.IoRegisters,
            Cpu.MemoryController.Oam,
            Cpu.MemoryController.HRam
        ];
        var index = 0;
        foreach (var fixedBank in fixedBanks)
        {
            memoryDump.AsSpan().Slice(index, fixedBank.MemorySpaceView.Length)
                .CopyTo(fixedBank.MemorySpaceView);
            
            index = index + fixedBank.MemorySpaceView.Length;
        }

        Cpu.Register.PC = memoryDump.AsSpan().Slice(index, length: 2).ToUShort();
        index += 2;
        Cpu.Register.SP = memoryDump.AsSpan().Slice(index, length: 2).ToUShort();
        index += 2;
        Cpu.Register.A = memoryDump[index++];
        Cpu.Register.B = memoryDump[index++];
        Cpu.Register.C = memoryDump[index++];
        Cpu.Register.D = memoryDump[index++];
        Cpu.Register.E = memoryDump[index++];
        Cpu.Register.H = memoryDump[index++];
        Cpu.Register.L = memoryDump[index++];
        Cpu.Register.F = memoryDump[index++];
        
        //Timers
        TimaTimer.TStatesCounter = memoryDump.AsSpan().Slice(index, length: 2).ToInt();
        index += 2;
        DivTimer.DividerCycleCounter = memoryDump.AsSpan().Slice(index, length: 2).ToInt();
        index +=2;
        
        //PPU
        Ppu.LyInternal = memoryDump[index++];
        Ppu._windowLineCounter = memoryDump[index++];
        Ppu._cyclesCounter = memoryDump.AsSpan().Slice(index, length: 2).ToInt();
        index += 2;
        Ppu._windowYConditionTriggered = memoryDump[index++] == 1;
        
        Apu.LeftMasterVolume = memoryDump[index++];
        Apu.RightMasterVolume = memoryDump[index++];
        Apu.IsAudioOn = memoryDump[index++] == 1;
        Apu.SampleCounter = memoryDump.AsSpan().Slice(index, length: 2).ToInt();
        index += 2;
        Apu._frameSequencerCyclesTimer = memoryDump.AsSpan().Slice(index, length: 2).ToInt();
        index += 2;
        Apu._frameSequencerPosition = memoryDump.AsSpan().Slice(index, length: 2).ToInt();
        index += 2;
        foreach (var channel in Apu.AvailableChannels)
        {
            channel.LoadRegistersFromDump(memoryDump, ref index);
        }
        
        MemoryController.IoRegisters._joypadRegister = memoryDump[index++];
        
        //CPU State
        Cpu.IsHalted = memoryDump[index++] == 1;
        Cpu.Register.InterruptsMasterEnabled = memoryDump[index++] == 1;
        Cpu.Register.IMEPending = memoryDump[index++] == 1;
        
        var mbc = MemoryController.RomBankNn;
        if (mbc is MemoryBankController mbcN)
        {
            mbcN.CurrentBank = memoryDump[index++];
            mbcN.ExternalRamAndRtcEnabled = memoryDump[index++] == 1;
            mbcN.RomBankingMode = memoryDump[index++];
            var totalRamSize = mbcN.ExternalRam.NumberOfBanks * mbcN.ExternalRam.BankSizeInBytes;
            memoryDump.AsSpan().Slice(index, totalRamSize).CopyTo(mbcN.ExternalRam.MemorySpaceView);
            index += totalRamSize;
        }
    }
}