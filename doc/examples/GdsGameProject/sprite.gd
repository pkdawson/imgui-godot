extends Sprite2D

func _ready() -> void:
    pass

func _process(delta: float) -> void:
    position.x += delta * 10.0

    if Engine.has_singleton("ImGuiAPI"):
        var ImGui: Object = Engine.get_singleton("ImGuiAPI")
        ImGui.Begin("SpriteTool: %s" % name, [], ImGui.WindowFlags_AlwaysAutoResize)
        var pos: Array = [position.x, position.y]
        if ImGui.DragInt2("position", pos):
            position = Vector2(pos[0], pos[1])
        ImGui.End()
