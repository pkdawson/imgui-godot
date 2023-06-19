@tool
extends EditorPlugin

func _enter_tree():
    add_autoload_singleton("ImGuiLayerNative", "res://addons/imgui-godot-native/imgui-godot.tscn")

func _exit_tree():
    remove_autoload_singleton("ImGuiLayerNative")
