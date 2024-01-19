#pragma once

#pragma warning(push, 0)
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/font_file.hpp>
#include <godot_cpp/classes/input_event.hpp>
#include <godot_cpp/classes/resource.hpp>
#include <godot_cpp/classes/sub_viewport.hpp>
#include <godot_cpp/classes/texture2d.hpp>
#include <godot_cpp/classes/window.hpp>
#include <godot_cpp/variant/callable.hpp>
#include <godot_cpp/variant/typed_array.hpp>
#pragma warning(pop)

using namespace godot;

namespace ImGui::Godot
{
//class Context
//{
//};

 void Init(Window* mainWindow, RID canvasItem, const Ref<Resource>& config);
 void Update(double delta);
 bool ProcessInput(const Ref<InputEvent>& evt, Window* window);
 void ProcessNotification(int what);
 void Render();
 void Shutdown();
 void Connect(const Callable& callable);
 void ResetFonts();
 void AddFont(const Ref<FontFile>& fontFile, int fontSize, bool merge = false);
 void AddFontDefault();
 void RebuildFontAtlas(float scale);
 void SetIniFilename(const String& fn);
 void SetVisible(bool visible);

 bool SubViewport(SubViewport* svp);

 void OnFramePreDraw();
}
