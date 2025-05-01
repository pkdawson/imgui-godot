using Godot;
#if GODOT_PC
using Hexa.NET.ImGui;
using System.Runtime.InteropServices;
using System;

namespace ImGuiGodot;

public partial class ImGuiSync : GodotObject
{
    public static readonly StringName GetImGuiPtrs = "GetImGuiPtrs";

    public static unsafe void SyncPtrs()
    {
        GodotObject gd = Engine.GetSingleton("ImGuiGD");
        long[] ptrs = (long[])gd.Call(GetImGuiPtrs,
            ImGui.GetVersionS(),
            sizeof(ImGuiIO),
            sizeof(ImDrawVert),
            sizeof(ushort),
            sizeof(uint)
            );

        if (ptrs.Length != 3)
        {
            throw new NotSupportedException("ImGui version mismatch");
        }

        checked
        {
            ImGui.SetCurrentContext(new((ImGuiContext*)(nint)ptrs[0]));
            ImGui.SetAllocatorFunctions(
                Marshal.GetDelegateForFunctionPointer<ImGuiMemAllocFunc>((nint)ptrs[1]),
                Marshal.GetDelegateForFunctionPointer<ImGuiMemFreeFunc>((nint)ptrs[2]));
        }
    }
}
#endif
