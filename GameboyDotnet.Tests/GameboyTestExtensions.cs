using FluentAssertions;
using GameboyDotnet.Memory;

namespace GameboyDotnet.Tests;

internal static class GameboyTestExtensions
{
    internal static void SetInitialState(this Gameboy gameboy, CpuTest test)
    {
        gameboy.Cpu.Register.A = test.Initial.A;
        gameboy.Cpu.Register.B = test.Initial.B;
        gameboy.Cpu.Register.C = test.Initial.C;
        gameboy.Cpu.Register.D = test.Initial.D;
        gameboy.Cpu.Register.E = test.Initial.E;
        gameboy.Cpu.Register.F = test.Initial.F;
        gameboy.Cpu.Register.H = test.Initial.H;
        gameboy.Cpu.Register.L = test.Initial.L;
        gameboy.Cpu.Register.PC = test.Initial.Pc;
        gameboy.Cpu.Register.SP = test.Initial.Sp;
        gameboy.Cpu.Register.InterruptsMasterEnabled = test.Initial.Ime == 1;
        gameboy.Cpu.MemoryController.WriteByte(BankAddress.InterruptEnableRegisterStart, test.Initial.Ie);
        
        foreach (var mem in test.Initial.Ram)
        {
            gameboy.Cpu.MemoryController.WriteByte((ushort)mem[0], (byte)mem[1]);
        }
    }

    internal static void CheckFinalState(this Gameboy gameboy, CpuTest test)
    {
        gameboy.Cpu.Register.A.Should().Be(test.Final.A);
        gameboy.Cpu.Register.B.Should().Be(test.Final.B);
        gameboy.Cpu.Register.C.Should().Be(test.Final.C);
        gameboy.Cpu.Register.D.Should().Be(test.Final.D);
        gameboy.Cpu.Register.E.Should().Be(test.Final.E);
        gameboy.Cpu.Register.F.Should().Be(test.Final.F);
        gameboy.Cpu.Register.H.Should().Be(test.Final.H);
        gameboy.Cpu.Register.L.Should().Be(test.Final.L);
        gameboy.Cpu.Register.PC.Should().Be(test.Final.Pc);
        gameboy.Cpu.Register.SP.Should().Be(test.Final.Sp);
        gameboy.Cpu.Register.InterruptsMasterEnabled.Should().Be(test.Final.Ime == 1);
        
        foreach (var mem in test.Final.Ram)
        {
            gameboy.Cpu.MemoryController.ReadByte((ushort)mem[0]).Should().Be((byte)mem[1]);
        }
    }

    internal static void Cleanup(this Gameboy gameboy, CpuTest test)
    {
        gameboy.Cpu.Register.AF = 0x01B0;
        gameboy.Cpu.Register.BC = 0x0013;
        gameboy.Cpu.Register.DE = 0x00D8;
        gameboy.Cpu.Register.HL = 0x014D;
        gameboy.Cpu.Register.SP = 0xFFFE;
        gameboy.Cpu.Register.PC = 0x0100;
        gameboy.Cpu.Register.InterruptsMasterEnabled = false;
        gameboy.Cpu.MemoryController.WriteByte(0xFFFF, 0);
        
        //Add final state to cleanup
        foreach (var mem in test.Initial.Ram.Concat(test.Final.Ram))
        {
            gameboy.Cpu.MemoryController.WriteByte((ushort)mem[0], 0);
        }
    }
}