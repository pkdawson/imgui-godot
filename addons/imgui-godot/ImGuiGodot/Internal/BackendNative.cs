#if GODOT_PC
using Godot;

namespace ImGuiGodot.Internal;

internal sealed class BackendNative : IBackend
{
    private readonly GodotObject _gd = Engine.GetSingleton("ImGuiGD");

    private sealed class MethodName
    {
        public static readonly StringName AddFont = "AddFont";
        public static readonly StringName AddFontDefault = "AddFontDefault";
        public static readonly StringName Connect = "Connect";
        public static readonly StringName RebuildFontAtlas = "RebuildFontAtlas";
        public static readonly StringName ResetFonts = "ResetFonts";
        public static readonly StringName SubViewport = "SubViewport";
        public static readonly StringName ToolInit = "ToolInit";
    }

    private sealed class PropertyName
    {
        public static readonly StringName JoyAxisDeadZone = "JoyAxisDeadZone";
        public static readonly StringName Scale = "Scale";
        public static readonly StringName Visible = "Visible";
    }

    public float JoyAxisDeadZone
    {
        get => throw new System.NotImplementedException();
        set => throw new System.NotImplementedException();
    }
    public float Scale { get; set; } = 1.0f; // TODO: make property
    public bool Visible
    {
        get => throw new System.NotImplementedException();
        set => throw new System.NotImplementedException();
    }

    public void AddFont(FontFile fontData, int fontSize, bool merge)
    {
        _gd.Call(MethodName.AddFont, fontData, fontSize, merge);
    }

    public void AddFontDefault()
    {
        _gd.Call(MethodName.AddFontDefault);
    }

    public void Connect(Callable callable)
    {
        _gd.Call(MethodName.Connect, callable);
    }

    public void RebuildFontAtlas(float scale)
    {
        _gd.Call(MethodName.RebuildFontAtlas, scale);
    }

    public void ResetFonts()
    {
        _gd.Call(MethodName.ResetFonts);
    }

    public bool SubViewportWidget(SubViewport svp)
    {
        return (bool)_gd.Call(MethodName.SubViewport, svp);
    }

    public void ToolInit()
    {
        _gd.Call(MethodName.ToolInit);
        ImGuiSync.SyncPtrs();
    }
}
#endif
