#if GODOT_PC
using Godot;

namespace ImGuiGodot.Internal;

internal sealed class BackendNative : IBackend
{
    public float JoyAxisDeadZone { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public float Scale { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public bool Visible { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public void AddFont(FontFile fontData, int fontSize, bool merge)
    {
        throw new System.NotImplementedException();
    }

    public void AddFontDefault()
    {
        throw new System.NotImplementedException();
    }

    public void Connect(Callable callable)
    {
        throw new System.NotImplementedException();
    }

    public void RebuildFontAtlas(float scale)
    {
        throw new System.NotImplementedException();
    }

    public void ResetFonts()
    {
        throw new System.NotImplementedException();
    }

    public bool SubViewportWidget(SubViewport svp)
    {
        throw new System.NotImplementedException();
    }
}
#endif
