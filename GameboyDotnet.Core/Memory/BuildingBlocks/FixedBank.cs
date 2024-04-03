﻿namespace GameboyDotnet.Components.Memory.BuildingBlocks;

public class FixedBank : IMemory
{
    public required int StartAddress { get; init; } = 0;
    public required int EndAddress { get; init; } = 0;
    public byte[] MemorySpace { get; init; }

    internal FixedBank()
    {
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

    public virtual ushort ReadWord(ref ushort address)
    {
        return (ushort)(MemorySpace[address - StartAddress] | (MemorySpace[address - StartAddress + 1] << 8));
    }

    public virtual void WriteWord(ref ushort address, ref ushort value)
    {
        MemorySpace[address - StartAddress] = (byte)(value & 0xFF);
        MemorySpace[address - StartAddress + 1] = (byte)(value >> 8);
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