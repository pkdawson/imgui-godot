extends Node

func _ready() -> void:
    if DisplayServer.window_get_vsync_mode() == DisplayServer.VSYNC_DISABLED:
        var refreshRate := DisplayServer.screen_get_refresh_rate()
        Engine.max_fps = int(refreshRate) if refreshRate > 0.0 else 60

    if Engine.has_singleton("ImGuiAPI"):
        var ImGui: Object = Engine.get_singleton("ImGuiAPI")
        var io: Object = ImGui.GetIO()
        io.ConfigFlags |= ImGui.ConfigFlags_ViewportsEnable

func _process(_delta: float) -> void:
    pass
