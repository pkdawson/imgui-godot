# [Dear ImGui](https://github.com/ocornut/imgui) plugin for [Godot 4](https://github.com/godotengine/godot) (C#)

![screenshot](doc/screenshot.png)

Dear ImGui is a popular library for rapidly building tools for debugging and development. This plugin, with the aid of [ImGui.NET](https://github.com/mellinoe/ImGui.NET), allows you to use ImGui in Godot with C#.

After installing the plugin, usage is as simple as this:
```csharp
public partial class MyNode : Node
{
    public override void _Ready()
    {
        ImGuiLayer.Instance.imgui_layout += _imgui_layout;
    }

    private void _imgui_layout()
    {
        ImGui.Begin("ImGui on Godot 4");
        ImGui.Text("hello world");
        ImGui.End();
    }
}
```

## Getting Started

### Demo

Click `Build` in the top right, then run the project.

On macOS, you will need to do something like:
```
cp .godot/mono/temp/bin/Debug/runtimes/osx-universal/native/libcimgui.dylib .
```

### Your project

1. Create a project and, if you haven't already added some C# code, use `Project > Tools > C# > Create C# solution`.

2. [Install the plugin](https://docs.godotengine.org/en/stable/tutorials/plugins/editor/installing_plugins.html) by copying over the `addons` folder.

3. In Visual Studio or another IDE, open the solution and allow unsafe code, and install `ImGui.NET` with NuGet. Save and return to Godot.

    (If you prefer to manually edit the .csproj instead, refer to the demo csproj for the necessary modifications, or copy it entirely.)

4. Back in the Godot editor, click `Build`.

5. Enable the plugin in `Project > Project Settings > Plugins`.

6. Write code!

## Usage

1. In any Node, connect to the `imgui_layout` signal in `_Ready` or `_EnterTree`:
    ```
    ImGuiLayer.Instance.imgui_layout += _imgui_layout;
    ```

2. In the method which handles this signal, use `ImGuiNET` to create your GUI.

The layout signal is emitted at the end of the processing step.

If you want to customize fonts or other settings, open the scene `res://addons/imgui-godot/ImGuiLayer.tscn`

Use the `Font` and `FontSize` properties to add a custom font. Use the `Visible` property to show/hide the GUI as needed. Change the `Layer` if you need to render anything on top of the GUI.

For custom textures, use the static methods `BindTexture` and `UnbindTexture` in `ImGuiGD`.

That's about it. Everything else is provided by ImGui itself, via ImGui.NET.

## Credits

All code written by Patrick Dawson, released under the MIT license

Godot Logo (C) Andrea Calabró, distributed under the terms of the Creative Commons Attribution 4.0 International License (CC-BY-4.0 International) https://creativecommons.org/licenses/by/4.0/

Hack font distributed under the [MIT license](https://github.com/source-foundry/Hack/blob/master/LICENSE.md)

M PLUS 2 font licensed under the SIL Open Font License, Version 1.1.

This plugin's functionality relies heavily on [ImGui.NET](https://github.com/mellinoe/ImGui.NET) by Eric Mellino
