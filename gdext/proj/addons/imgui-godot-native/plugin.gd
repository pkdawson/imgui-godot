@tool
extends EditorPlugin

func _enter_tree():
    print("plugin ET")
    if ClassDB.class_exists("ImGuiGD"):
        print("plugin init")
        var root = get_editor_interface().get_base_control().get_node("/root")
        ClassDB.instantiate("ImGuiGD").call("InitEditor", root)
    add_autoload_singleton("ImGuiGodot", "res://addons/imgui-godot-native/ImGuiGodot.tscn")

func _exit_tree():
    remove_autoload_singleton("ImGuiGodot")
