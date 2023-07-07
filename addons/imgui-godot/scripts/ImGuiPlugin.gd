@tool
extends EditorPlugin

func _enter_tree():
    var features = ProjectSettings.get_setting("application/config/features")
    if "C#" in features:
        add_autoload_singleton("imgui_godot", "res://addons/imgui-godot/ImGuiLayer.tscn")

func _exit_tree():
    if ProjectSettings.has_setting("autoload/imgui_godot"):
        remove_autoload_singleton("imgui_godot")
