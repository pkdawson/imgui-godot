#pragma once

#pragma warning(push, 0)
#include <godot_cpp/variant/variant.hpp>
#pragma warning(pop)

#include <memory>
#include <vector>

using namespace godot;

namespace ImGui::Godot {

class ShortTermCache
{
public:
    ShortTermCache();
    ~ShortTermCache();

    void OnNewFrame();
    std::vector<char>& GetTextBuf(const StringName& label, size_t size, const Array& a);
    const std::vector<char>& GetZeroArray(const Array& a);

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

extern std::unique_ptr<ShortTermCache> gdscache;

} // namespace ImGui::Godot
