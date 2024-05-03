#pragma once
#include <godot_cpp/classes/font_file.hpp>
#include <imgui.h>
#include <memory>
using namespace godot;

namespace ImGui::Godot {

class Fonts
{
public:
    Fonts();
    ~Fonts();

    void Reset();
    void Add(Ref<FontFile> fontData, int fontSize, bool merge = false, const ImVector<ImWchar>& glyphRanges = {});
    void RebuildFontAtlas(float scale);

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
