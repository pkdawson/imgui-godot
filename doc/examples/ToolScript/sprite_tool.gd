@tool
extends Node2D

var show_tool_gui := ImGuiGD.ToolInit()
var myscale := [scale.x]
var myrot := [0]
var auto_rotate := [false]

func _ready():
    if show_tool_gui:
        var io = ImGui.GetIO()
        io.ConfigFlags |= ImGui.ConfigFlags_ViewportsEnable
        io.ConfigViewportsNoAutoMerge = true
    

func _process(_delta):
    if show_tool_gui:
        ImGui.Begin("tool: " + name)
        if ImGui.DragFloatEx("scale", myscale, 0.1, 0.25, 8.0):
            scale.x = myscale[0]
            scale.y = myscale[0]
            
        myrot[0] = rad_to_deg(rotation)
        if ImGui.DragInt("rotation", myrot):
            rotation = deg_to_rad(myrot[0])
        
        ImGui.Checkbox("auto rotate", auto_rotate)
        if auto_rotate[0]:
            if rotation >= 2 * PI:
                rotation = 0
            rotation += deg_to_rad(1)
        ImGui.End()
