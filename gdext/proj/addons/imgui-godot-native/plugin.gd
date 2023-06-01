@tool
extends EditorPlugin

func _enter_tree():
    # The autoload can be a scene or script file.
    add_autoload_singleton("imgui_godot_native", "res://addons/imgui-godot-native/imgui-godot.tscn")


func _exit_tree():
    remove_autoload_singleton("imgui_godot_native")
