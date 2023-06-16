@tool
extends EditorPlugin

func _enter_tree():
    # if "C#" in ProjectSettings.get_setting("application/config/features"):
    if Engine.has_singleton("GodotSharp"):
        add_autoload_singleton("ImGuiLayer", "res://addons/imgui-godot/ImGuiLayer.tscn")

func _exit_tree():
    remove_autoload_singleton("ImGuiLayer")
