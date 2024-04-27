# Dear ImGui plugin for Godot 4 (C#)

![](https://img.shields.io/static/v1?label=Godot&message=4.2&color=blue&logo=godotengine)

![](https://github.com/pkdawson/imgui-godot/actions/workflows/dotnet.yml/badge.svg)
![](https://github.com/pkdawson/imgui-godot/actions/workflows/godot.yml/badge.svg)

![screenshot](doc/screenshot.png)

[Dear ImGui](https://github.com/ocornut/imgui) is a popular library for rapidly building tools for debugging and development. This plugin, with the aid of [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET), allows you to use ImGui in Godot with C#, C++, and GDScript.

After installing the plugin, usage is as simple as this:

```csharp
public partial class MyNode : Node
{
    public override void _Process(double delta)
    {
        ImGui.Begin("ImGui on Godot 4");
        ImGui.Text("hello world");
        ImGui.End();
    }
}
```

Download

[![](https://img.shields.io/static/v1?label=imgui-godot&message=latest%20release&color=blueviolet&logo=github)](https://github.com/pkdawson/imgui-godot/releases/latest)

## Getting Started (C#)

1. Create a project and, if you haven't already added some C# code, use `Project > Tools > C# > Create C# solution`.

2. [Install the plugin](https://docs.godotengine.org/en/stable/tutorials/plugins/editor/installing_plugins.html) by copying over the `addons` folder. Or [use GodotEnv](#package-managers).

3. In Visual Studio or another IDE, open the solution and allow unsafe code, and install `ImGui.NET` with NuGet. Save and return to Godot.

    (If you prefer to manually edit the .csproj instead, refer to the demo csproj for the necessary modifications.)

4. Back in the Godot editor, click `Build`.

5. Enable the plugin in `Project > Project Settings > Plugins`.

6. Write code!

## Usage

In any Node's `_Process` method, use `ImGuiNET` to create your GUI. Just don't set the `ProcessPriority` in any of your Nodes to either `int.MinValue` or `int.MaxValue`.

### Signals

You can also connect to the `ImGuiLayout` signal, and use ImGui in the method which handles that signal. This is strongly recommended if you're using process thread groups in Godot 4.1 or later.

```csharp
ImGuiGD.Connect(OnImGuiLayout);
```

### Configuration

If you want to customize fonts or other settings, create an `ImGuiConfig` resource, then open the scene `res://addons/imgui-godot/ImGuiLayer.tscn` and set its `Config` property.

### Widgets

These methods should only be called within `_Process` or an `ImGuiLayout` callback.

`Image` and `ImageButton` are simple wrappers for your convenience.

`SubViewport` displays an interactive viewport which receives input events. Be sure to change your SubViewport's `Update Mode` to **Always**.

### ImGuiGD

This is the rest of the public API. You typically won't need to call any of these methods directly.

That's about it. Everything else is provided by ImGui itself, via ImGui.NET.

### Mobile export

ImGui.NET does not support iOS, Android, or web, so all ImGui related code should be conditionally disabled if you want to export for these platforms. For example:

```csharp
#if GODOT_PC
ImGui.Begin("my window");
// ...
ImGui.End();
#endif
```

## Package managers

[GodotEnv](https://github.com/chickensoft-games/GodotEnv/) is a dotnet tool that can manage Godot addons with just a little configuration. Use something like:

```json
{
  "addons": {
    "imgui-godot": {
      "url": "https://github.com/pkdawson/imgui-godot",
      "checkout": "4.x",
      "subfolder": "addons/imgui-godot"
    }
  }
}
```

## Credits

Code written by Patrick Dawson and contributors, released under the MIT license

Godot Logo (C) Andrea Calabró, distributed under the terms of the Creative Commons Attribution 4.0 International License (CC-BY-4.0 International) <https://creativecommons.org/licenses/by/4.0/>

Hack font distributed under the [MIT license](https://github.com/source-foundry/Hack/blob/master/LICENSE.md)

M PLUS 2 font licensed under the SIL Open Font License, Version 1.1.

This plugin's functionality relies heavily on [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET) by Eric Mellino
