using Godot;
using System;
using System.Runtime.InteropServices;

namespace ImGuiGodot.Internal;

internal static class Util
{
    public static Rid ConstructRid(ulong id)
    {
        ReadOnlySpan<ulong> uspan = new(in id);
        ReadOnlySpan<byte> bytes = MemoryMarshal.Cast<ulong, byte>(uspan);
        return MemoryMarshal.Read<Rid>(bytes);
    }
}
