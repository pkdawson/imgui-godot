#include "ImGuiGodot.h"

namespace ImGui::Godot {

struct ImGuiGodot::Impl
{
    Object* cfg;
};

ImGuiGodot::ImGuiGodot() : impl(std::make_unique<Impl>())
{
}

ImGuiGodot::~ImGuiGodot()
{
}

void ImGuiGodot::_bind_methods()
{
    ClassDB::bind_method(D_METHOD("SetConfig", "cfg"), &ImGuiGodot::SetConfig);
    ClassDB::bind_method(D_METHOD("GetConfig"), &ImGuiGodot::GetConfig);
    ADD_PROPERTY(PropertyInfo(Variant::OBJECT, "Config", PROPERTY_HINT_RESOURCE_TYPE, "ImGuiConfig"),
                 "SetConfig",
                 "GetConfig");
}

void ImGuiGodot::_get_property_list(List<PropertyInfo>* p_list) const
{
    p_list->push_back(PropertyInfo(Variant::OBJECT, "Config", PROPERTY_HINT_RESOURCE_TYPE, "ImGuiConfig"));
}

void ImGuiGodot::_enter_tree()
{
}

void ImGuiGodot::SetConfig(Object* cfg)
{
    impl->cfg = cfg;
}

Object* ImGuiGodot::GetConfig()
{
    return nullptr;
}

} // namespace ImGui::Godot
