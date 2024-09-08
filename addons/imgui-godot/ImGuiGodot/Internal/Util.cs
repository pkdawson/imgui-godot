using Godot;
using System;

namespace ImGuiGodot.Internal;

internal static class Util
{
    public static unsafe Rid ConstructRid(ulong id)
    {
        Rid rv;
        Buffer.MemoryCopy(&id, &rv, sizeof(Rid), sizeof(ulong));
        return rv;
    }
}
