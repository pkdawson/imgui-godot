Run, for example:

```
git clone https://github.com/ocornut/imgui
git -C imgui checkout v1.91.6-docking
git clone https://github.com/godotengine/godot
cd godot
scons custom_modules=../modules
cd ../project
godotenv addons install
../godot/bin/godot.* -e
```
