namespace GameboyDotnet.SDL.SaveStates;

public static class SaveDumper
{
    public static void SaveState(Gameboy gameboy, string romPath)
    {
        var savePath = romPath.Replace(".gb", ".gbsav");
        try
        {
            File.WriteAllBytes(savePath, gameboy.DumpMemory());
            Console.WriteLine("Saved state");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }
    
    public static void LoadState(Gameboy gameboy, string romPath)
    {
        var savePath = romPath.Replace(".gb", ".gbsav");
        try
        {
            var saveState = File.ReadAllBytes(savePath);
            gameboy.LoadMemoryDump(saveState);
            Console.WriteLine("Loaded save state");
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}