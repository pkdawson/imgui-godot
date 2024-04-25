#pragma once
#include <memory>

#include <godot_cpp/classes/font_file.hpp>
using namespace godot;

namespace ImGui::Godot {

class Fonts
{
public:
    Fonts();
    ~Fonts();

    void Reset();
    void Add(Ref<FontFile> fontData, int fontSize, bool merge = false);
    void RebuildFontAtlas(float scale);

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot