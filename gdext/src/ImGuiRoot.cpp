#include "ImGuiRoot.h"
#include "ImGuiLayer.h"
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/window.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

namespace ImGui::Godot {

struct ImGuiRoot::Impl
{
    Ref<Resource> cfg;
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
}

void ImGuiRoot::_enter_tree()
{
    Node* parent = get_parent();
    if (parent == get_window() || parent->get_name() == StringName("ImGuiGodotNativeEditorPlugin"))
    {
        Engine::get_singleton()->register_singleton("ImGuiRoot", this);
        ImGuiLayer* igl = memnew(ImGuiLayer);
        add_child(igl);
    }
}

void ImGuiRoot::SetConfig(Ref<Resource> cfg)
{
    impl->cfg = cfg;
}

Ref<Resource> ImGuiRoot::GetConfig()
{
    return impl->cfg;
}

} // namespace ImGui::Godot