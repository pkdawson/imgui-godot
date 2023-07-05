@tool
extends EditorPlugin

func _enter_tree():
    ImGuiGD.InitEditor()
    add_autoload_singleton("imgui_godot_native", "res://addons/imgui-godot-native/ImGuiGodot.tscn")

func _exit_tree():
    remove_autoload_singleton("imgui_godot_native")
