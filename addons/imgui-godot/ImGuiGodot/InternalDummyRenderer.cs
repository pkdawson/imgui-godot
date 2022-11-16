using Godot;
using ImGuiNET;

namespace ImGuiGodot;

internal class InternalDummyRenderer : IRenderer
{
    public string Name => "imgui_impl_godot4_dummy";

    public void Init(ImGuiIOPtr io)
    {
    }

    public void InitViewport(Viewport vp)
    {
    }

    public void CloseViewport(Viewport vp)
    {
    }

    public void OnHide()
    {
    }

    public void RenderDrawData(Viewport vp, ImDrawDataPtr drawData)
    {
    }

    public void Shutdown()
    {
    }
}
