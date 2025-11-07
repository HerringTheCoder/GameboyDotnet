using System.Buffers;
using GameboyDotnet.Memory.BuildingBlocks;

namespace GameboyDotnet.Tools;

public static class SaveDumper
{
    public static string RomPath = string.Empty;
    public static void SaveState(Gameboy gameboy)
    {
        var savePath = RomPath.Replace(".gb", ".gbsav");
        try
        {
            var totalExternalRamSize = 0;
            var saveFileSize = 64 * 1024; //Base memory size
            if (gameboy.MemoryController.RomBankNn is MemoryBankController mbc)
            {
                totalExternalRamSize = mbc.ExternalRam.NumberOfBanks * mbc.ExternalRam.BankSizeInBytes;
                saveFileSize += totalExternalRamSize;
            }
            var sharedArray = ArrayPool<byte>.Shared.Rent(saveFileSize);
            
            gameboy.DumpWritableMemory(sharedArray.AsSpan(), totalExternalRamSize);
            File.WriteAllBytes(savePath, sharedArray.AsSpan(0, saveFileSize));
            Console.WriteLine("Saved state");
            
            ArrayPool<byte>.Shared.Return(sharedArray);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
    }
    
    public static void LoadState(Gameboy gameboy)
    {
        var savePath = RomPath.Replace(".gb", ".gbsav");
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