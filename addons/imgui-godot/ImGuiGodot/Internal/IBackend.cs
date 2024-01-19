using Godot;

namespace ImGuiGodot.Internal;

internal interface IBackend
{
    public float JoyAxisDeadZone { get; set; }
    public float Scale { get; set; }
    public void ResetFonts();
    public void AddFont(FontFile fontData, int fontSize, bool merge);
    public void AddFontDefault();
    public void RebuildFontAtlas(float scale);
    public void Connect(Callable callable);
    public bool SubViewportWidget(SubViewport svp);
}
