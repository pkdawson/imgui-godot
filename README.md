# imgui-godot
[Godot](https://github.com/godotengine/godot) plugin for integrating [Dear ImGui](https://github.com/ocornut/imgui)

Still a work in progress, some unfinished stuff, not sure about the interface, probably bugs, resource leaks, etc

## Getting Started

### Demo

On Windows, just click `Build` in the top right then play the project. On other platforms, you may need to
run `nuget restore`, then try the build again.

### Your project

C# support in Godot is somewhat incomplete, so I've tried to fill in the gaps with a script that runs
when you enable the plugin.

**WARNING:** This could corrupt your .csproj file. Always use source control, or at least make a backup first.

1. Create a project and click `Build` in the top right to generate the .csproj file.

2. [Install the plugin](https://docs.godotengine.org/en/stable/tutorials/plugins/editor/installing_plugins.html) by copying over the `addons` folder.

3. Enable the plugin in `Project > Project Settings > Plugins`.

4. When prompted, click OK to patch your .csproj file.

    (Otherwise, you'll have to modify it yourself: add the .cs scripts in addons\imgui-godot, allow unsafe blocks, and install ImGui.NET with NuGet.)

5. Click `Build` again (if you get errors, you probably need to run `nuget restore`).

6. Add an `ImGuiNode` to your scene.

7. Write code!

## Interface

I've provided two ways to use ImGui. See the demo project scenes for examples.

### Signals

1. Drop an `ImGuiNode` wherever you want in your scene (usually near the end, so it's rendered on top).

2. From a script on any other node (or multiple nodes!), connect the `IGLayout` signal.

3. In the function which handles this signal, use `ImGuiNET` to create your GUI.

### Script extension

If you need to override something, or if you just want to do everything with one node:

1. Add the `ImGuiNode`, then use `Extend Script`.

2. The Godot prompt won't let you inherit from `ImGuiNode`, so be sure to fix that after your new script is created.

3. Override `Layout` - see `MyGui.cs` for details. Be careful when overriding other methods; it should be
ok if you make sure to call the parent method first (using `base`).

Since this plugin is unfinished, it doesn't make sense to document an API yet. Check the samples and use the static methods provided by ImGuiGD.

## Project export

When exporting your project with Godot, the native code cimgui library won't be included.
On Windows, you can just copy over cimgui.dll (from .mono\temp\bin\ExportRelease) to the same directory as your exe.

On macOS, I haven't been able to get the .app to work no matter where I put the .dylib. It does work if you run the binary directly.

## Credits

All code written by Patrick Dawson, released to the public domain (Creative Commons Zero v1.0 Universal)

Godot Logo (C) Andrea Calabró Distributed under the terms of the Creative Commons Attribution License version 3.0 (CC-BY 3.0) https://creativecommons.org/licenses/by/3.0/legalcode.

Hack font distributed under the [MIT license](https://github.com/source-foundry/Hack/blob/master/LICENSE.md)

This plugin's functionality relies heavily on [ImGui.NET](https://github.com/mellinoe/ImGui.NET) by Eric Mellino
