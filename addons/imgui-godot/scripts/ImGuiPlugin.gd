@tool
extends EditorPlugin

func _enter_tree():
    if Engine.has_singleton("GodotSharp"):
        add_autoload_singleton("imgui_godot", "res://addons/imgui-godot/ImGuiLayer.tscn")

func _exit_tree():
    remove_autoload_singleton("imgui_godot")
