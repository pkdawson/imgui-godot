extends Node

@onready var tex: Texture2D = preload("res://icon.svg")

var open := [true]

func _ready():
    ImGuiGD.Connect(_on_imgui_layout)
    var io = ImGui.GetIO()
    io.ConfigFlags |= ImGui.ConfigFlags_NavEnableKeyboard

func _on_imgui_layout():
    ImGui.SetNextWindowPos(Vector2i(200, 200), ImGui.Cond_Once)
    ImGui.Begin("signal")
    ImGui.Text("from signal")
    ImGui.End()
