extends Node

signal imgui_layout

const csharp_layer := "res://addons/imgui-godot/ImGuiGodot/ImGuiLayer.cs"
const csharp_sync := "res://addons/imgui-godot/ImGuiGodot/ImGuiSync.cs"

func _enter_tree():
    var features := ProjectSettings.get_setting("application/config/features")
    if ClassDB.class_exists("ImGuiLayerNative"):
        add_child(ClassDB.instantiate("ImGuiLayerNative"))
        if "C#" in features:
            add_child(load(csharp_sync).new())
    else:
        if "C#" in features:
            add_child(load(csharp_layer).new())
