@tool
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

func _process(_delta):
    if open[0]:
        myfloat[0] = myfloat[0] + 0.001
        buf[0] = buf[0] + 'X'
        ImGui.Begin("gds", open, ImGui.WindowFlags_MenuBar)
        ImGui.BeginMenuBar()
        if ImGui.BeginMenu("Menu"):
            ImGui.MenuItem("hello")
            ImGui.EndMenu()
        ImGui.EndMenuBar()
        ImGui.Text("hello")
        ImGui.SliderFloat("myfloat", myfloat, 0.0, 1.0)
        ImGui.DragFloat("dragf", myfloat)
        ImGui.ProgressBar(myfloat[0], Vector2(-1, 0), "prog")
        ImGui.InputText("stuff1", buf, 32)
        ImGui.InputText("stuff2", buf2, 16)
        ImGui.PushID("foo")
        ImGui.InputText("stuff1", buf3, 32)
        ImGui.PopID()
        ImGuiGD.Image(tex, Vector2(128, 128))
        if ImGuiGD.ImageButton("mybtn", tex, Vector2(128, 128)):
            print("click")
        ImGui.End()

func _on_imgui_layout():
    ImGui.SetNextWindowPos(Vector2i(200, 200))
    ImGui.Begin("signal")
    ImGui.Text("from signal")
    ImGui.End()
