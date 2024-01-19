extends Node

signal imgui_layout

const csharp_layer := "res://addons/imgui-godot/ImGuiGodot/ImGuiLayer.cs"
const csharp_sync := "res://addons/imgui-godot/ImGuiGodot/ImGuiSync.cs"

func _enter_tree():
    Engine.register_singleton("ImGuiRoot", self)
    var features := ProjectSettings.get_setting("application/config/features")
    if ClassDB.class_exists("ImGuiLayer"):
        # native
        add_child(ClassDB.instantiate("ImGuiLayer"))
        if "C#" in features:
            var obj: Object = load(csharp_sync).new()
            obj.SyncPtrs()
            obj.free()
    else:
        # C# only
        if "C#" in features:
            add_child(load(csharp_layer).new())
