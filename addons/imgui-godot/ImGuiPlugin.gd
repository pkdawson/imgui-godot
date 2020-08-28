tool
extends EditorPlugin

func _enter_tree():
    add_custom_type("ImGuiNode", "Node2D", preload("ImGuiNode.cs"), preload("icon.tres"))

func _exit_tree():
    remove_custom_type("ImGuiNode")
