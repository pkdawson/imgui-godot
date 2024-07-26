extends Node

var myfloat := [0.0]
var mystr := [""]
var values := [2.0, 4.0, 0.0, 3.0, 1.0, 5.0]
var items := ["zero", "one", "two", "three"]
var current_item := [2]
var anim_counter := 0
var wc_topmost: ImGuiWindowClassPtr

func _ready():
	var io := ImGui.GetIO()
	io.ConfigFlags |= ImGui.ConfigFlags_ViewportsEnable

	wc_topmost = ImGuiWindowClassPtr.new()
	wc_topmost.ViewportFlagsOverrideSet = ImGui.ViewportFlags_TopMost | ImGui.ViewportFlags_NoAutoMerge

	var style := ImGui.GetStyle()
	style.Colors[ImGui.Col_PlotHistogram] = Color.REBECCA_PURPLE
	style.Colors[ImGui.Col_PlotHistogramHovered] = Color.SLATE_BLUE

func _process(_delta: float) -> void:
	ImGui.ShowDemoWindow()

	ImGui.Begin("hello")
	ImGui.Text("hello from GDScript")
	ImGui.DragFloat("myfloat", myfloat)
	ImGui.Text(str(myfloat[0]))
	ImGui.InputText("mystr", mystr, 32)
	ImGui.Text(mystr[0])

	ImGui.PlotHistogram("histogram", values, values.size())
	ImGui.PlotLines("lines", values, values.size())
	ImGui.ListBox("choices", current_item, items, items.size())
	ImGui.Combo("combo", current_item, items)
	ImGui.Text("choice = %s" % items[current_item[0]])
	ImGui.End()

	ImGui.SetNextWindowClass(wc_topmost)
	ImGui.SetNextWindowSize(Vector2(200, 200), ImGui.Cond_Once)
	ImGui.Begin("topmost viewport window")
	ImGui.TextWrapped("when this is a viewport window outside the main window, it will stay on top")
	ImGui.End()

func _physics_process(_delta: float) -> void:
	anim_counter += 1
	if anim_counter >= 10:
		anim_counter = 0
		values.push_back(values.pop_front())
