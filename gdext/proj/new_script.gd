extends Node

@onready var tex: Texture2D = preload("res://icon.svg")

var p_open := [true]
var p_selected := [false]
var p_item = [1]
var p_listitem = [0]
var items = ["Zero", "One", "Two", "Three", "Four", "Five", "Six"]

func _ready():
    ImGuiGD.Connect(_on_imgui_layout)
    var io = ImGui.GetIO()
    io.ConfigFlags |= ImGui.ConfigFlags_NavEnableKeyboard

func _process(_delta):
    if p_open[0]:
        show_demo()

func show_demo():
    ImGui.Begin("GDScript Demo", p_open, ImGui.WindowFlags_MenuBar)
    if ImGui.BeginMenuBar():
        if ImGui.BeginMenu("File"):
            if ImGui.MenuItem("New"):
                print("clicked New")
            ImGui.MenuItemBoolPtr("checkable", "", p_selected)
            ImGui.EndMenu()
        ImGui.EndMenuBar()


    ImGui.Combo("combo", p_item, items)
    ImGui.Text("Selected item: " + items[p_item[0]])
    ImGui.ListBox("listbox", p_listitem, items, items.size(), 3)
    ImGui.Text("Selected item: " + items[p_listitem[0]])
    ImGui.End()

    ImGui.ShowDemoWindow()

func _on_imgui_layout():
    ImGui.SetNextWindowPos(Vector2i(200, 200), ImGui.Cond_Once)
    ImGui.Begin("signal")
    ImGui.Text("from signal")
    ImGui.End()
