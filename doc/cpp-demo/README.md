
A sample GDExtension using ImGui with a context from imgui-godot. Uses vcpkg and CMake for easy setup and fast builds.

## Windows (MSVC)
```
cmake --preset msvc.debug
cmake --build msvc.debug --config Debug --target install

cmake --preset msvc.release
cmake --build msvc.release --config RelWithDebInfo --target install
```

TODO: fonts with FreeType

TODO: macOS build
