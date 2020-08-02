# imgui-godot
[Godot](https://github.com/godotengine/godot) plugin for integrating [Dear ImGui](https://github.com/ocornut/imgui)

## Getting Started

When first opening the demo project, you'll get an error about being unable to load the addon script.
Just open the `Mono` tab at the bottom of the editor, click `Build Project`, then go into
`Project > Project Settings > Plugins` and re-enable the plugin.

Installing this in your own project is extremely finicky (as of Godot 3.2.3, with C# support in "late alpha").
I've tried to figure out and document the necssary steps.

1. Create a project with a C# script to enable Mono, so Godot generates the .csproj file.

2. [Install the plugin](https://docs.godotengine.org/en/stable/tutorials/plugins/editor/installing_plugins.html) by copying over the `addons` folder.

3. Notice that you can't enable the plugin.

4. Time to manually edit the .csproj. Refer to the demo csproj for guidance. Add these lines:
```xml
    <Compile Include="addons\imgui-godot\ImGuiNode.cs" />
    <Compile Include="addons\imgui-godot\ImGuiPlugin.cs" />
```

5. While you're here, you can enable unsafe code and add `ImGui.NET` as a NuGet package. Or do that in Visual Studio if you prefer.

Add this to all three configurations:
```xml
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
```

Add this with the other PackageReferences:

```xml
    <PackageReference Include="ImGui.NET">
      <Version>1.75.0</Version>
    </PackageReference>
```

6. Back in Godot, do `Mono > Build Project`

7. Enable the plugin in `Project Settings`

8. Add an `ImGuiNode` to your scene.

9. Write code finally!


## Interface

