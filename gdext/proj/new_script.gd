extends Node

@onready var tex: Texture2D = preload("res://icon.svg")
var open := [true]

func _ready():
    ImGuiGD.Connect(_on_imgui_layout)
    var io = ImGui.GetIO()
    io.ConfigFlags |= ImGui.ConfigFlags_ViewportsEnable

func _process(_delta):
    if open[0]:
        ImGui.Begin("gds", open)
        ImGui.Text("hello")
        ImGuiGD.Image(tex, Vector2(128, 128))
        ImGuiGD.ImageButton("mybtn", tex, Vector2(128, 128))
        ImGui.End()

func _on_imgui_layout():
    ImGui.SetNextWindowPos(Vector2i(200, 200))
    ImGui.Begin("signal")
    ImGui.Text("from signal")
    ImGui.End()
