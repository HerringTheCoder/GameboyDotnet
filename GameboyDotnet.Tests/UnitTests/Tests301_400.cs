using FluentAssertions.Execution;
using GameboyDotnet.Common;
using Microsoft.Extensions.Logging;

namespace GameboyDotnet.Tests.UnitTests;

public class Tests301_400
{
    public static IEnumerable<object[]> TestCases()
    {
        foreach (var test in TestHelper.TestCases(300, 100))
        {
            yield return [test.Key, test.Value];
        }
    }

    [Theory]
    [MemberData(nameof(TestCases))]
    public void Test1(string fileName, List<CpuTest> tests)
    {
        var gameboy = new Gameboy(LoggerHelper.GetLogger<Gameboy>(LogLevel.None));
        
        foreach (var test in tests)
        {
            gameboy.SetInitialState(test);
            gameboy.Cpu.ExecuteNextOperation();
            using (new AssertionScope())
            {
                gameboy.CheckFinalState(test);
            }
            gameboy.Cleanup(test);
        }
    }  
}