extends Node

@onready var tex: Texture2D = preload("res://icon.svg")
@onready var myfloat := [0.0]
var open := [true]
var buf := ["here's buf 1"]
var buf2 := [""]
var buf3 = [""]

func _ready():
    print("ET")
    ImGuiGD.Connect(_on_imgui_layout)
    var io = ImGui.GetIO()
    io.ConfigFlags |= ImGui.ConfigFlags_NavEnableKeyboard

func _on_imgui_layout():
    ImGui.SetNextWindowPos(Vector2i(200, 200))
    ImGui.Begin("signal")
    ImGui.Text("from signal")
    ImGui.End()
