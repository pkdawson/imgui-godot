#pragma once
#include <godot_cpp/variant/variant.hpp>
#include <memory>
#include <vector>

using namespace godot;

namespace ImGui::Godot {

class GdsCache
{
public:
    GdsCache();
    ~GdsCache();

    void OnNewFrame();
    std::vector<char>& GetTextBuf(const StringName& label, size_t size, const Array& a);
    const std::vector<char>& GetZeroArray(const Array& a);

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

extern std::unique_ptr<GdsCache> gdscache;

} // namespace ImGui::Godot
