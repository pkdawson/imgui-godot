extends "res://test_base.gd"

var frame := 0

func _ready() -> void:
    pass

func _process(_delta: float) -> void:
    if frame == 0:
        frame_zero()
    else:
        frame_one()
    frame += 1

func frame_zero() -> void:
    # ImGuiConfig loads properly
    assert_equal(ImGuiGD.Scale, 2)
    assert_equal(ImGui.GetFontSize(), 26)

    # rescale
    call_deferred("change_scale")

func frame_one() -> void:
    assert_equal(ImGuiGD.Scale, 4)
    assert_equal(ImGui.GetFontSize(), 52)

    exit_with_status()

func change_scale() -> void:
    ImGuiGD.Scale = 4
    ImGuiGD.RebuildFontAtlas()
