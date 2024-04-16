using FluentAssertions.Execution;
using GameboyDotnet.Common;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Tests.UnitTests;

public class SingularTests
{
    public static IEnumerable<object[]> TestCases()
    {
        foreach (var test in TestHelper.TestCases(0, 500, fileName: "d9.json").SelectMany(x => x.Value))
        {
            yield return [test];
        }
    }

    // [Theory]
    // [MemberData(nameof(TestCases))]
    // public void Test1(CpuTest test)
    // {
    //     var gameboy = new Gameboy(LoggerHelper.GetLogger<Gameboy>(LogLevel.None));
    //
    //     gameboy.SetInitialState(test);
    //     gameboy.Cpu.ExecuteNextOperation();
    //     using (new AssertionScope())
    //     {
    //         gameboy.CheckFinalState(test);
    //     }
    //
    //     gameboy.Cleanup(test);
    // }
}