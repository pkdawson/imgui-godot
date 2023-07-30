using Godot;
using ImGuiNET;

namespace ImGuiGodot.Internal;

public interface IPublicInterface
{
    public void SetJoyAxisDeadZone(float zone);
    public void SetJoyButtonSwapAB(bool swap);
    public void SetVisible(bool visible);

    public void Init(Window mainWindow, Rid mainSubViewport, Resource cfg);
    public void Update(double delta, Vector2 displaySize);
    public bool ProcessInput(InputEvent evt, Window window);
    public void Render();
    public void Shutdown();

    public void Connect(Callable callable);

    public void ResetFonts();
    public void AddFont(FontFile fontData, int fontSize, bool merge);
    public void RebuildFontAtlas(float scale);

    public void SetIniFilename(ImGuiIOPtr io, string fileName);
    public void SyncImGuiPtrs();
    public bool ToolInit();
    public bool SubViewport(SubViewport vp);
}
