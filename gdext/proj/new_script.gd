extends Node

func _ready():
    $/root/imgui_godot.imgui_layout.connect(_on_imgui_layout)

func _process(delta):
    ImGui.Begin("gds")
    ImGui.Text("hello")
    ImGui.End()
    
func _on_imgui_layout():
    ImGui.Begin("signal")
    ImGui.Text("from signal")
    ImGui.End()
