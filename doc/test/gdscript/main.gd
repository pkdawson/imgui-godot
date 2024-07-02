extends "res://test_base.gd"

signal within_process

func _ready() -> void:
    await within_process

    # ImGuiConfig loads properly
    assert_equal(ImGuiGD.Scale, 2)
    assert_equal(ImGui.GetFontSize(), 26)

    # rescale
    call_deferred("change_scale")

    await within_process

    assert_equal(ImGuiGD.Scale, 4)
    assert_equal(ImGui.GetFontSize(), 52)

    exit_with_status()

func _process(_delta: float) -> void:
    within_process.emit()

func change_scale() -> void:
    ImGuiGD.Scale = 4
    ImGuiGD.RebuildFontAtlas()
