#pragma once

#pragma warning(push, 0)
#include <godot_cpp/classes/node.hpp>
#pragma warning(pop)

#include <memory>

using namespace godot;

namespace ImGui::Godot {

class ImGuiRoot : public Node
{
    GDCLASS(ImGuiRoot, Node);

protected:
    static void _bind_methods();
    void _get_property_list(List<PropertyInfo>* p_list) const;

public:
    ImGuiRoot();
    ~ImGuiRoot();

    void _enter_tree() override;

    void SetConfig(Object* cfg);
    Object* GetConfig();

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
