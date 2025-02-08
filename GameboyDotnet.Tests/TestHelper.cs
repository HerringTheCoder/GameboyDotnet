using System.Text.Json;
using FluentAssertions;

namespace GameboyDotnet.Tests;

public static class TestHelper
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IDictionary<string, List<CpuTest>> TestCases(
        int skip,
        int take,
        string? fileName = null,
        string? specificTestName = null)
    {
        var directory = new DirectoryInfo(@"F:\Emulators\Gameboy\TestOutput\jsmoo-json-tests\tests\sm83\v1");
        var files = directory.GetFiles("*.json");
        var allTests = new Dictionary<string, List<CpuTest>>();
        var fileQuery = files.Skip(skip).Take(take).Where(x =>
            fileName is null || x.Name.Equals(fileName, StringComparison.InvariantCultureIgnoreCase));

        foreach (var file in fileQuery)
        {
            using var stream = file.OpenRead();

            var tests = JsonSerializer.Deserialize<List<CpuTest>>(stream, DefaultJsonOptions)!;
            allTests.Add(file.Name, tests.Where(x => x.Name.Equals(specificTestName, StringComparison.InvariantCultureIgnoreCase)).ToList());
        }

        return allTests;
    }

    internal static void SetInitialState(Gameboy gameboy, CpuTest test)
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
        foreach (var mem in test.Initial.Ram)
        {
            gameboy.Cpu.MemoryController.WriteByte((ushort)mem[0], (byte)mem[1]);
        }
    }

    internal static void CheckFinalState(Gameboy gameboy, CpuTest test)
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
}