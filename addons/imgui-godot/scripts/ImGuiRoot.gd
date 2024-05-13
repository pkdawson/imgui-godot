extends Node

signal imgui_layout

const csharp_controller := "res://addons/imgui-godot/ImGuiGodot/ImGuiController.cs"
const csharp_sync := "res://addons/imgui-godot/ImGuiGodot/ImGuiSync.cs"

func _enter_tree():
    # Engine.register_singleton("ImGuiRoot", self)
    var features := ProjectSettings.get_setting("application/config/features")
    if ClassDB.class_exists("ImGuiController"):
        # native
        add_child(ClassDB.instantiate("ImGuiController"))
        if "C#" in features:
            var obj: Object = load(csharp_sync).new()
            obj.SyncPtrs()
            obj.free()
    else:
        # C# only
        if "C#" in features:
            add_child(load(csharp_controller).new())
