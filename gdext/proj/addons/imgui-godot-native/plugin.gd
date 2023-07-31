@tool
extends EditorPlugin

func _enter_tree():
    name = "ImGuiGodotNativeEditorPlugin"
    if not ClassDB.class_exists("ImGuiGD"):
        push_warning("imgui-godot-native: restarting editor to complete installation")
        get_editor_interface().restart_editor()
        return
    var igd = Engine.get_singleton("ImGuiGD")
    igd.InitEditor(self)
    add_autoload_singleton("imgui_godot_native", "res://addons/imgui-godot-native/ImGuiGodot.tscn")

func _exit_tree():
    remove_autoload_singleton("imgui_godot_native")
