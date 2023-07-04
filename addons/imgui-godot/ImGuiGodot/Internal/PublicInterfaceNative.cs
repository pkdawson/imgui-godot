using Godot;
using ImGuiNET;

namespace ImGuiGodot.Internal;

public class PublicInterfaceNative : IPublicInterface
{
    private readonly GodotObject _gd = (GodotObject)ClassDB.Instantiate("ImGuiGD");

    public void AddFont(FontFile fontData, int fontSize, bool merge)
    {
        _gd.Call("AddFont", fontData, fontSize, merge);
        throw new System.NotImplementedException();
    }

    public void Connect(Callable callable)
    {
        throw new System.NotImplementedException();
    }

    public void Init(Window mainWindow, Rid mainSubViewport, Resource cfg)
    {
        throw new System.NotImplementedException();
    }

    public bool ProcessInput(InputEvent evt, Window window)
    {
        throw new System.NotImplementedException();
    }

    public void RebuildFontAtlas(float scale)
    {
        throw new System.NotImplementedException();
    }

    public void Render()
    {
        throw new System.NotImplementedException();
    }

    public void ResetFonts()
    {
        throw new System.NotImplementedException();
    }

    public void SetIniFilename(ImGuiIOPtr io, string fileName)
    {
        throw new System.NotImplementedException();
    }

    public void SetJoyAxisDeadZone(float zone)
    {
        throw new System.NotImplementedException();
    }

    public void SetJoyButtonSwapAB(bool swap)
    {
        throw new System.NotImplementedException();
    }

    public void SetScale(float scale)
    {
        throw new System.NotImplementedException();
    }

    public void Shutdown()
    {
        throw new System.NotImplementedException();
    }

    public void Update(double delta, Vector2 displaySize)
    {
        throw new System.NotImplementedException();
    }
}
