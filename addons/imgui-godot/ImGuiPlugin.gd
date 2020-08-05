tool
extends EditorPlugin

func _enter_tree():
    add_custom_type("ImGuiNode", "Node2D", preload("ImGuiNode.cs"), preload("icon.tres"))

    var name = ProjectSettings.get_setting("application/config/name")
    fix_csproj("res://" + name + ".csproj")

func _exit_tree():
    remove_custom_type("ImGuiNode")

func prompt(fn):
    var dlg = ConfirmationDialog.new()
    dlg.window_title = "imgui-godot"
    dlg.dialog_text = "Your .csproj will be modified to support imgui-godot. Is that ok?"
    dlg.connect("modal_closed", dlg, "queue_free")
    dlg.connect("confirmed", self, "fix_csproj", [fn, true])
    get_editor_interface().get_base_control().add_child(dlg)
    dlg.popup_centered()

func fix_csproj(fn, really=false):
    # this is very silly, never do this
    var needSources = true
    var needNuget = true
    var needUnsafe = true

    var fi = File.new()
    if fi.open(fn, File.READ) != OK:
        printerr("imgui-godot: could not open " + fn)
        return

    var fileLines = []
    while not fi.eof_reached():
        var line = fi.get_line()
        fileLines.append(line)

        if line.find("<Compile") != -1:
            if line.find("ImGuiNode.cs") != -1:
                needSources = false
        elif line.find("<PackageReference") != -1:
            if line.find("ImGui.NET") != -1:
                needNuget = false
        elif line.find("<AllowUnsafeBlocks>") != -1:
            needUnsafe = false
    fi.close()

    if needSources or needNuget or needUnsafe:
        if not really:
            prompt(fn)
            return
        else:
            print("imgui-godot: fixing ", fn)
    else:
        return

    fi.open(fn, File.WRITE)
    var i = 0
    for line in fileLines:
        if needSources and line.find("<Compile") != -1:
            needSources = false
            fi.store_line("    <Compile Include=\"addons\\imgui-godot\\ImGuiGD.cs\" />")
            fi.store_line("    <Compile Include=\"addons\\imgui-godot\\ImGuiNode.cs\" />")
            fi.store_line(line)
        elif needNuget and line.find("<PackageReference") != -1:
            needNuget = false
            fi.store_line("    <PackageReference Include=\"ImGui.NET\">")
            fi.store_line("      <Version>1.75.0</Version>")
            fi.store_line("    </PackageReference>")
            fi.store_line(line)
        elif needUnsafe and line.find("<ConsolePause>") != -1:
            fi.store_line(line)
            fi.store_line("    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>")
        elif i == fileLines.size() - 1:
            # avoid adding an extra newline at the end
            fi.store_string(line)
        else:
            fi.store_line(line)
        i += 1
    fi.close()
