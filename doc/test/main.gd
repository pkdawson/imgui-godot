extends "res://test_base.gd"

func _ready() -> void:
    pass

func _process(_delta: float) -> void:
    # ImGuiConfig loads properly
    assert_equal(ImGui.GetFontSize(), 26)

    exit_with_status()
