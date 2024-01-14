#if GODOT_PC
using Godot;
using ImGuiNET;

namespace ImGuiGodot.Internal;

internal sealed class DummyRenderer : IRenderer
{
    public string Name => "godot4_net_dummy";

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
#endif
