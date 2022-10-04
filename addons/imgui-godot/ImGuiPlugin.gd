@tool
extends EditorPlugin

func _enter_tree():
    add_autoload_singleton("ImGuiLayer", "res://addons/imgui-godot/ImGuiLayer.tscn")

func _exit_tree():
    remove_autoload_singleton("ImGuiLayer")
