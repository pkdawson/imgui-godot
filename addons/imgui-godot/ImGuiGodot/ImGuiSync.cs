using Godot;
#if GODOT_PC
using ImGuiNET;
using System.Runtime.InteropServices;
using System;

namespace ImGuiGodot;

public partial class ImGuiSync : GodotObject
{
    public static void SyncPtrs()
    {
        GodotObject gd = Engine.GetSingleton("ImGuiGD");
        long[] ptrs = (long[])gd.Call("GetImGuiPtrs",
            ImGui.GetVersion(),
            Marshal.SizeOf<ImGuiIO>(),
            Marshal.SizeOf<ImDrawVert>(),
            sizeof(ushort),
            sizeof(ushort)
            );

        if (ptrs.Length != 3)
            return;

        checked
        {
            ImGui.SetCurrentContext((IntPtr)ptrs[0]);
            ImGui.SetAllocatorFunctions((IntPtr)ptrs[1], (IntPtr)ptrs[2]);
        }
    }
}
#endif
