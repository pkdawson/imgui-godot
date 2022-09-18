# [Dear ImGui](https://github.com/ocornut/imgui) plugin for [Godot 4](https://github.com/godotengine/godot) (C#)

![screenshot](doc/screenshot.png)

Dear ImGui is a popular library for rapidly building tools for debugging and development. This plugin, with the aid of [ImGui.NET](https://github.com/mellinoe/ImGui.NET), allows you to use ImGui in Godot with C#.

After a little setup, usage is as simple as this:
```csharp
public partial class MyNode : Node
{
    public void _on_imgui_layout()
    {
        ImGui.Begin("ImGui on Godot 4");
        ImGui.Text("hello world");
        ImGui.End();
    }
}
```

## Getting Started

### Demo

As of Godot 4.0 beta 1, you'll probably get a warning when you open the project. Just click Ok, click Build, then go to `Project > Project Settings > Plugins` and enable the plugin.

Click `Build` in the top right, then play the project.

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

6. Add an `ImGuiNode` to your scene.

7. Write code!

## Usage

1. Drop an `ImGuiNode` wherever you want in your scene (usually near the end, so it's rendered on top).

2. From a script on any other node (or multiple nodes!), connect the `imgui_layout` signal.

3. In the method which handles this signal, use `ImGuiNET` to create your GUI.

Use the `Font` and `FontSize` properties to add a custom font. `ImGuiNode` respects the `Visible` property, so that's the best way to hide the GUI as needed.

For custom textures, use the static methods `BindTexture` and `UnbindTexture` in `ImGuiGD`.

That's about it. Everything else is provided by ImGui itself, via ImGui.NET.

## Credits

All code written by Patrick Dawson, released under the MIT license

Godot Logo (C) Andrea Calabró, distributed under the terms of the Creative Commons Attribution 4.0 International License (CC-BY-4.0 International) https://creativecommons.org/licenses/by/4.0/

Hack font distributed under the [MIT license](https://github.com/source-foundry/Hack/blob/master/LICENSE.md)

This plugin's functionality relies heavily on [ImGui.NET](https://github.com/mellinoe/ImGui.NET) by Eric Mellino
