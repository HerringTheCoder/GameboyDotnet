namespace GameboyDotnet.Tests;

public class CpuTest
{
    public string Name { get; set; }
    public InitialState Initial { get; set; }
    public FinalState Final { get; set; }
    public List<List<object>> Cycles { get; set; }
}

public class InitialState
{
    public ushort Pc { get; set; }
    public ushort Sp { get; set; }
    public byte A { get; set; }
    public byte B { get; set; }
    public byte C { get; set; }
    public byte D { get; set; }
    public byte E { get; set; }
    public byte F { get; set; }
    public byte H { get; set; }
    public byte L { get; set; }
    public byte Ime { get; set; }
    public byte Ie { get; set; }
    public List<List<int>> Ram { get; set; }
}

public class FinalState
{
    public ushort Pc { get; set; }
    public ushort Sp { get; set; }
    public byte A { get; set; }
    public byte B { get; set; }
    public byte C { get; set; }
    public byte D { get; set; }
    public byte E { get; set; }
    public byte F { get; set; }
    public byte H { get; set; }
    public byte L { get; set; }
    public byte Ime { get; set; }
    public List<List<int>> Ram { get; set; }
}

        
        