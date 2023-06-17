extends Node

func _ready():
    $/root/imgui_godot.imgui_layout.connect(_on_imgui_layout)
    var io = ImGui.GetIO()
    io.ConfigFlags |= ImGui.ConfigFlags_ViewportsEnable

func _process(_delta):
    ImGui.Begin("gds")
    ImGui.Text("hello")
    ImGui.End()

func _on_imgui_layout():
    ImGui.SetNextWindowPos(Vector2i(200, 200))
    ImGui.Begin("signal")
    ImGui.Text("from signal")
    ImGui.End()
