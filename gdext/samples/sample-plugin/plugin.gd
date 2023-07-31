@tool
extends EditorPlugin

func _enter_tree():
    # Initialization of the plugin goes here.
    pass
    
func _ready():
    ImGuiGD.ToolInit()
    
func _process(_delta):
    ImGui.Begin("plugin window")
    ImGui.Text("hello from plugin")
    ImGui.End()

func _exit_tree():
    # Clean-up of the plugin goes here.
    pass
