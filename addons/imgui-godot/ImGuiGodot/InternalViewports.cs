#if IMGUI_GODOT_DEV
using ImGuiNET;

namespace ImGuiGodot;

internal static class InternalViewports
{
    public static void Init(ImGuiIOPtr io)
    {
        io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
    }
}
#endif
