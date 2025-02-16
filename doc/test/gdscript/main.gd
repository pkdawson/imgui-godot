extends "res://test_base.gd"

signal within_process

func _ready() -> void:
    ImGuiGD.SetIniFilename("")

    await within_process

    # ImGuiConfig loads properly
    assert_equal(ImGuiGD.Scale, 2)
    assert_equal(ImGui.GetFontSize(), 26)

    # StringName conversion
    assert_equal(ImGui.GetID("test_id"), 3584119329)

    # rescale
    call_deferred("change_scale")

    await within_process

    assert_equal(ImGuiGD.Scale, 4)
    assert_equal(ImGui.GetFontSize(), 52)

    # IniSavingRate
    get_tree().create_timer(5.1).timeout.connect(on_timeout)

func on_timeout():
    await within_process

    assert_false(FileAccess.file_exists("user://imgui.ini"))

    exit_with_status()

func _process(_delta: float) -> void:
    within_process.emit()

func change_scale() -> void:
    ImGuiGD.Scale = 4
    ImGuiGD.RebuildFontAtlas()
