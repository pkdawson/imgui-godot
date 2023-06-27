#include "ImGuiRoot.h"
#include "ImGuiLayer.h"
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/window.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

namespace ImGui::Godot {

struct ImGuiRoot::Impl
{
    Object* cfg;
};

ImGuiRoot::ImGuiRoot() : impl(std::make_unique<Impl>())
{
}

ImGuiRoot::~ImGuiRoot()
{
}

void ImGuiRoot::_bind_methods()
{
    ClassDB::bind_method(D_METHOD("SetConfig", "cfg"), &ImGuiRoot::SetConfig);
    ClassDB::bind_method(D_METHOD("GetConfig"), &ImGuiRoot::GetConfig);
    ADD_PROPERTY(PropertyInfo(Variant::OBJECT, "Config", PROPERTY_HINT_RESOURCE_TYPE, "ImGuiConfig"),
                 "SetConfig",
                 "GetConfig");
}

void ImGuiRoot::_get_property_list(List<PropertyInfo>* p_list) const
{
    // p_list->push_back(PropertyInfo(Variant::OBJECT, "Config", PROPERTY_HINT_RESOURCE_TYPE, "ImGuiConfig"));
}

void ImGuiRoot::_enter_tree()
{
    if (get_parent() == get_window())
    {
        Engine::get_singleton()->register_singleton("ImGuiRoot", this);
        ImGuiLayer* igl = memnew(ImGuiLayer);
        add_child(igl);
    }
}

void ImGuiRoot::SetConfig(Object* cfg)
{
    impl->cfg = cfg;
}

Object* ImGuiRoot::GetConfig()
{
    return nullptr;
}

} // namespace ImGui::Godot
