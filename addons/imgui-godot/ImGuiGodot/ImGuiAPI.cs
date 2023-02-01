#if IMGUI_GODOT_DEV
using Godot;
using ImGuiNET;
using System;
using System.Diagnostics;
using Array = Godot.Collections.Array;

namespace ImGuiGodot;

// can this be generic? no specialization like C++
// maybe use a source generator
internal class GdRefBool : IDisposable
{
    private bool _value;
    private readonly Array _array;

    public ref bool Ref => ref _value;

    public GdRefBool(Array array)
    {
        Debug.Assert(array.Count == 1);
        Debug.Assert(array[0].VariantType == Variant.Type.Bool);
        _array = array;
        _value = _array[0].AsBool();
    }

    public void Dispose()
    {
        _array[0] = _value;
    }
}

#pragma warning disable CA1822 // Mark members as static
public partial class ImGuiAPI : Godot.Object
{
    // this can be done with reflection
    public int WindowFlags_None = (int)ImGuiWindowFlags.None;
    public int WindowFlags_NoTitleBar = (int)ImGuiWindowFlags.NoTitleBar;

    public bool Begin(string name)
    {
        return ImGui.Begin(name);
    }

    private static bool _Begin(string name, Array p_open)
    {
        using GdRefBool open = new(p_open);
        return ImGui.Begin(name, ref open.Ref);
    }

    private static bool _Begin(string name, int flags)
    {
        return ImGui.Begin(name, (ImGuiWindowFlags)flags);
    }

    public bool Begin(string name, Variant arg2) => arg2.VariantType switch
    {
        Variant.Type.Array => _Begin(name, arg2.AsGodotArray()),
        Variant.Type.Int => _Begin(name, arg2.AsInt32()),
        _ => throw new ArgumentException($"invalid variant type {arg2.VariantType}", nameof(arg2))
    };

    public bool Begin(string name, Array p_open, int flags)
    {
        using GdRefBool open = new(p_open);
        return ImGui.Begin(name, ref open.Ref, (ImGuiWindowFlags)flags);
    }

    public void End()
    {
        ImGui.End();
    }

    public void Text(string txt)
    {
        ImGui.Text(txt);
    }

    public bool Button(string label)
    {
        return ImGui.Button(label);
    }

    public bool Button(string label, Vector2 size)
    {
        return ImGui.Button(label, new(size.X, size.Y));
    }
}
#pragma warning restore CA1822 // Mark members as static
#endif
