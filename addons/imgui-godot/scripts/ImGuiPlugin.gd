@tool
extends EditorPlugin

func _enter_tree():
    if ProjectSettings.has_setting("autoload/ImGuiLayer"):
        remove_autoload_singleton("ImGuiLayer")
    add_autoload_singleton("ImGuiRoot", "res://addons/imgui-godot/data/ImGuiRoot.tscn")
    Engine.register_singleton("ImGuiPlugin", self)

func _exit_tree():
    remove_autoload_singleton("ImGuiRoot")
    Engine.unregister_singleton("ImGuiPlugin")
