#if GODOT_PC
using Godot;

namespace ImGuiGodot.Internal;

internal interface IRenderer
{
    public string Name { get; }
    public void InitViewport(Rid vprid);
    public void CloseViewport(Rid vprid);
    public void RenderDrawData();
    public void OnHide();
    public void Shutdown();
}
#endif
