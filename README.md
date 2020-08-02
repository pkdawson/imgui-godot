# imgui-godot
[Godot](https://github.com/godotengine/godot) plugin for integrating [Dear ImGui](https://github.com/ocornut/imgui)

Still a work in progress, some unfinished stuff, not sure about the interface, probably bugs, resource leaks, etc

## Getting Started

When first opening the demo project, you'll get an error about being unable to load the addon script.
Just click `Build` in the top right corner, then go into `Project > Project Settings > Plugins` and
re-enable the plugin.

Installing this in your own project is extremely finicky (as of Godot 3.2.3, with C# support in "late alpha").
I've tried to figure out and document the necssary steps.

1. Create a project and click `Build` in the top right to generate the .csproj file.

2. [Install the plugin](https://docs.godotengine.org/en/stable/tutorials/plugins/editor/installing_plugins.html) by copying over the `addons` folder.

3. Notice that you can't enable the plugin.

4. Well, time to manually edit the .csproj. Refer to the demo csproj for guidance. Add these lines next to the other Compile tags:
```xml
    <Compile Include="addons\imgui-godot\ImGuiNode.cs" />
    <Compile Include="addons\imgui-godot\ImGuiPlugin.cs" />
```

5. While you're here, you can enable unsafe code and add `ImGui.NET` as a NuGet package. Or do that in Visual Studio if you prefer.

Add this to all three configurations (probably below ConsolePause):
```xml
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
```

Add this with the other PackageReference tags:

```xml
    <PackageReference Include="ImGui.NET">
      <Version>1.75.0</Version>
    </PackageReference>
```

6. Back in Godot, click `Build` again.

7. Enable the plugin in `Project Settings`.

8. Add an `ImGuiNode` to your scene.

9. Write code finally!

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

## Project export

When exporting your project with Godot, the native code cimgui library won't be included.
On Windows, you can just copy over cimgui.dll (from .mono\temp\bin\ExportRelease) to the same directory as your exe.

On macOS, I haven't been able to get the .app to work no matter where I put the .dylib. It does work if you run the binary directly.
