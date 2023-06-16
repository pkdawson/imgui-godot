using Godot;
using ImGuiNET;

namespace ImGuiGodot.Internal;

internal sealed class DummyRenderer : IRenderer
{
    public string Name => "imgui_impl_godot4_dummy";

    public void Init(ImGuiIOPtr io)
    {
    }

    public void InitViewport(Rid vprid)
    {
    }

    public void CloseViewport(Rid vprid)
    {
    }

    public void OnHide()
    {
    }

    public void RenderDrawData()
    {
    }

    public void Shutdown()
    {
    }
}
