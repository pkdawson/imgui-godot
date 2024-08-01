extends Node

var myfloat := [0.0]
var mystr := [""]
var values := [2.0, 4.0, 0.0, 3.0, 1.0, 5.0]
var items := ["zero", "one", "two", "three", "four", "five"]
var current_item := [2]
var anim_counter := 0
var wc_topmost: ImGuiWindowClassPtr
var ms_items := items
var ms_selection := []

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

    var gdver: String = Engine.get_version_info()["string"]

    ImGui.Begin("Demo")
    ImGui.Text("ImGui in")
    ImGui.SameLine()
    ImGui.TextLinkOpenURLEx("Godot %s" % gdver, "https://www.godotengine.org")
    ImGui.Text("mem %.1f KiB / peak %.1f KiB" % [
        OS.get_static_memory_usage() / 1024.0,
        OS.get_static_memory_peak_usage() / 1024.0])
    ImGui.Separator()

    ImGui.DragFloat("myfloat", myfloat)
    ImGui.Text(str(myfloat[0]))
    ImGui.InputText("mystr", mystr, 32)
    ImGui.Text(mystr[0])

    ImGui.PlotHistogram("histogram", values, values.size())
    ImGui.PlotLines("lines", values, values.size())
    ImGui.ListBox("choices", current_item, items, items.size())
    ImGui.Combo("combo", current_item, items)
    ImGui.Text("choice = %s" % items[current_item[0]])

    ImGui.SeparatorText("Multi-Select")
    var ms_io := ImGui.BeginMultiSelectEx(ImGui.MultiSelectFlags_None, ms_items.size(), ms_selection.size())
    apply_selection_requests(ms_io)
    for i in range(items.size()):
        var is_selected := ms_selection.has(i)
        ImGui.SetNextItemSelectionUserData(i)
        ImGui.SelectableEx(ms_items[i], is_selected)
    ms_io = ImGui.EndMultiSelect()
    apply_selection_requests(ms_io)
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

func apply_selection_requests(ms_io: ImGuiMultiSelectIOPtr) -> void:
    for req: ImGuiSelectionRequestPtr in ms_io.Requests:
        if req.Type == ImGui.SelectionRequestType_SetAll:
            if req.Selected:
                ms_selection = range(ms_items.size())
            else:
                ms_selection.clear()
        elif req.Type == ImGui.SelectionRequestType_SetRange:
            for i in range(req.RangeFirstItem, req.RangeLastItem + 1):
                if req.Selected:
                    ms_selection.append(i)
                else:
                    ms_selection.erase(i)
