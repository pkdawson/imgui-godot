#pragma once
#include <godot_cpp/classes/canvas_layer.hpp>
#include <godot_cpp/classes/display_server.hpp>
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/font_file.hpp>
#include <godot_cpp/classes/image.hpp>
#include <godot_cpp/classes/image_texture.hpp>
#include <godot_cpp/classes/input_event.hpp>
#include <godot_cpp/classes/project_settings.hpp>
#include <godot_cpp/classes/rendering_server.hpp>
#include <godot_cpp/classes/resource.hpp>
#include <godot_cpp/classes/sub_viewport.hpp>
#include <godot_cpp/classes/texture2d.hpp>
#include <godot_cpp/classes/viewport_texture.hpp>
#include <godot_cpp/classes/window.hpp>
#include <godot_cpp/variant/callable.hpp>
#include <godot_cpp/variant/packed_byte_array.hpp>
#include <godot_cpp/variant/typed_array.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

#include "DummyRenderer.h"
#include "Fonts.h"
#include "GdsCache.h"
#include "ImGuiGD.h"
#include "ImGuiLayer.h"
#include "Input.h"
#include "RdRenderer.h"
#include "RdRendererThreadSafe.h"
#include "Renderer.h"
#include "Viewports.h"

using namespace godot;

namespace ImGui::Godot {

enum class RendererType
{
    Dummy,
    Canvas,
    RenderingDevice,
};

struct Context
{
    std::unique_ptr<Viewports> viewports;
    std::unique_ptr<Fonts> fonts;
    std::unique_ptr<Input> input;
    std::unique_ptr<Renderer> renderer;

    float scale = 1.0f;
    float joyAxisDeadZone = 0.15f;
    int layerNum = 128;
    ImGuiLayer* layer = nullptr;
    bool inProcessFrame = false;
    std::vector<char> iniFilename;

    Context(std::unique_ptr<Renderer> r);
    ~Context();

    void Render();
    void Update(double delta, Vector2 displaySize);
};

Context* GetContext();

void Init(const Ref<Resource>& config);
void Shutdown();
void Connect(const Callable& callable);
void ResetFonts();
void AddFont(const Ref<FontFile>& fontFile, int fontSize, bool merge = false,
             const ImVector<ImWchar>& glyphRanges = {});
void AddFontDefault();
void RebuildFontAtlas();
void SetIniFilename(const String& fn);
void SetVisible(bool visible);

bool SubViewportWidget(SubViewport* svp);
} // namespace ImGui::Godot
