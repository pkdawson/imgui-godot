extends Node

var myfloat := [0.0]
var mystr := [""]

func _ready():
    var io := ImGui.GetIO()
    io.ConfigFlags |= ImGui.ConfigFlags_ViewportsEnable

func _process(_delta):
    ImGui.Begin("hello")
    ImGui.Text("hello from GDScript")
    ImGui.DragFloat("myfloat", myfloat)
    ImGui.Text(str(myfloat[0]))
    ImGui.InputText("mystr", mystr, 32)
    ImGui.Text(mystr[0])
    ImGui.End()
