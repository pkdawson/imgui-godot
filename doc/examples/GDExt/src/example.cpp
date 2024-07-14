#include "example.h"
#include "gdmarkdown.h"
#include <godot_cpp/classes/resource_loader.hpp>
#include <imgui-godot.h>
#include <implot.h>

using godot::Engine;
using godot::ResourceLoader;

static const std::string markdownText = R"(# H1 Header

Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.

## H2 Header

Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.

### H3 Header

  * Item 1
  * Item 2
  * Item 3

*Emphasis* and **strong emphasis** change the appearance of the text.

Link: [Godot Engine](https://godotengine.org)

robot eye ![an atlas texture](res://robot_eye.tres)

___

big robot
![](res://icon.svg)
)";

void Example::_bind_methods()
{
}

Example::Example()
{
}

Example::~Example()
{
}

void Example::_ready()
{
#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif

    _img = ResourceLoader::get_singleton()->load("res://robot_eye.tres");
    ImGui::InitMarkdown();
}

void Example::_process(double delta)
{
#ifdef DEBUG_ENABLED
    if (Engine::get_singleton()->is_editor_hint())
        return;
#endif

    static int x = 0;

    ImGui::SetNextWindowSize({200, 200}, ImGuiCond_Once);
    ImGui::Begin("GDExtension Example");
    ImGui::DragInt("x", &x);
    ImGui::Text("x = %d", x);
    ImGui::Separator();
    ImGui::Image(_img, {64, 64});
    ImGui::End();

    ImGui::ShowDemoWindow();

    ImPlot::ShowDemoWindow();

    ImGui::Begin("Markdown example");
    ImGui::Markdown(markdownText);
    ImGui::End();
}
