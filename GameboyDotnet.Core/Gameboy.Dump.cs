using Microsoft.Extensions.Logging;

namespace GameboyDotnet;

public partial class Gameboy
{
    public byte[] DumpMemory()
    {
        var memoryDump = new byte[(0xFFFF + 1) + 12 + 2]; //Address space + 6 registers + 2 timers + 1 Ly
        for(int i = 0; i < memoryDump.Length; i++)
        {
            memoryDump[i] = Cpu.MemoryController.ReadByte((ushort)i);
        }
        memoryDump[0xFFFF + 1] = (byte)(Cpu.Register.PC & 0xFF);
        memoryDump[0xFFFF + 2] = (byte)(Cpu.Register.PC >> 8);
        memoryDump[0xFFFF + 3] = (byte)(Cpu.Register.SP & 0xFF);
        memoryDump[0xFFFF + 4] = (byte)(Cpu.Register.SP >> 8);
        memoryDump[0xFFFF + 5] = Cpu.Register.A;
        memoryDump[0xFFFF + 6] = Cpu.Register.B;
        memoryDump[0xFFFF + 7] = Cpu.Register.C;
        memoryDump[0xFFFF + 8] = Cpu.Register.D;
        memoryDump[0xFFFF + 9] = Cpu.Register.E;
        memoryDump[0xFFFF + 10] = Cpu.Register.H;
        memoryDump[0xFFFF + 11] = Cpu.Register.L;
        memoryDump[0xFFFF + 12] = Cpu.Register.F;
        IsMemoryDumpRequested = false;
        
        _logger.LogWarning("Memory dump created");
        
        return memoryDump;
    }
    
    public void LoadMemoryDump(byte[] dump)
    {
        IsMemoryDumpRequested = true;
        for (int i = 0; i <= 0xFFFF; i++)
        {
            Cpu.MemoryController.WriteByte((ushort)i, dump[i]);
        }
        Cpu.Register.PC = (ushort)(dump[0xFFFF + 1] | (dump[0xFFFF + 2] << 8));
        Cpu.Register.SP = (ushort)(dump[0xFFFF + 3] | (dump[0xFFFF + 4] << 8));
        Cpu.Register.A = dump[0xFFFF + 5];
        Cpu.Register.B = dump[0xFFFF + 6];
        Cpu.Register.C = dump[0xFFFF + 7];
        Cpu.Register.D = dump[0xFFFF + 8];
        Cpu.Register.E = dump[0xFFFF + 9];
        Cpu.Register.H = dump[0xFFFF + 10];
        Cpu.Register.L = dump[0xFFFF + 11];
        Cpu.Register.F = dump[0xFFFF + 12];
        IsMemoryDumpRequested = false;
    }
}