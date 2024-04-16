﻿using GameboyDotnet.Components.Memory.BuildingBlocks;

namespace GameboyDotnet.Memory.BuildingBlocks;

public class FixedBank : IMemory
{
    public int StartAddress { get; init; } = 0;
    public int EndAddress { get; init; } = 0;
    public byte[] MemorySpace { get; init; }
    public string Name { get; init; }
    
    public FixedBank(int startAddress, int endAddress, string name)
    {
        StartAddress = startAddress;
        EndAddress = endAddress;
        Name = name;
        MemorySpace = new byte[EndAddress - StartAddress + 1];
    }
    
    public virtual byte ReadByte(ref ushort address)
    {
        return MemorySpace[address - StartAddress];
    }

    public virtual void WriteByte(ref ushort address, ref byte value)
    {
        MemorySpace[address - StartAddress] = value;
    }

    public virtual void IncrementByte(ref ushort memoryAddress)
    {
        MemorySpace[memoryAddress - StartAddress]++;
    }
    
    public virtual void DecrementByte(ref ushort memoryAddress)
    {
        MemorySpace[memoryAddress - StartAddress]--;
    }
}