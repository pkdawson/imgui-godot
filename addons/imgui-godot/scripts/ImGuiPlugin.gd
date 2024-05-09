@tool
extends EditorPlugin

func _enter_tree():
    if ProjectSettings.has_setting("autoload/ImGuiLayer"):
        remove_autoload_singleton("ImGuiLayer")
    add_autoload_singleton("ImGuiRoot", "res://addons/imgui-godot/data/ImGuiRoot.tscn")
    Engine.register_singleton("ImGuiPlugin", self)
    
    if "C#" in ProjectSettings.get_setting("application/config/features"):
        var projPath: String = ProjectSettings.get_setting("dotnet/project/solution_directory")
        var fn: String = "%s.csproj" % ProjectSettings.get_setting("dotnet/project/assembly_name")
        if projPath != "":
            fn = "%s/%s" % [projPath, fn]
        check_csproj(fn)
        
func check_csproj(fn):
    var fi := FileAccess.open(fn, FileAccess.READ)
    if !fi:
        return

    var changesNeeded := ""
    var data := fi.get_as_text()
    var idx := data.find("<TargetFramework>net")
    if idx != -1:
        idx += len("<TargetFramework>net")
        var idx_dot := data.find(".", idx)
        var netVer := data.substr(idx, idx_dot - idx).to_int()
        if netVer < 8:
            changesNeeded += "- Set target framework to .NET 8 or later\n"
            
    idx = data.find("<AllowUnsafeBlocks>True")
    if idx == -1:
        changesNeeded += "- Allow unsafe blocks\n"
    
    idx = data.find("<PackageReference Include=\"ImGui.NET\"")
    if idx == -1:
        changesNeeded += "- Add NuGet package ImGui.NET\n"
        
    if changesNeeded != "":
        var text := "Your .csproj requires the following changes:\n\n%s" % changesNeeded
        push_warning("imgui-godot\n\n%s" % text)

func _exit_tree():
    remove_autoload_singleton("ImGuiRoot")
    Engine.unregister_singleton("ImGuiPlugin")
