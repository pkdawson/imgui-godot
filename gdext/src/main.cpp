#include <godot_cpp/classes/display_server.hpp>
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/main_loop.hpp>
#include <godot_cpp/classes/scene_tree.hpp>
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/core/defs.hpp>
#include <godot_cpp/core/version.hpp>
#include <godot_cpp/godot.hpp>
#include <godot_cpp/variant/utility_functions.hpp>

static_assert(GODOT_VERSION_MAJOR == 4 && GODOT_VERSION_MINOR >= 2);

#include <imgui.h>

#include "ImGuiGD.h"
#include "ImGuiLayer.h"
#include "ImGuiLayerHelper.h"
#include "Viewports.h"

// avoid including cimgui.h elsewhere

namespace ImGui::Godot {
void register_imgui_api();
}

using namespace godot;
using namespace ImGui::Godot;

ImGuiGD* gd = nullptr;

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#else
#include <dlfcn.h>
#endif

void sync_modules()
{
    typedef void (*pmodinit)(uint32_t, ImGuiContext*, ImGuiMemAllocFunc, ImGuiMemFreeFunc);
#ifdef _WIN32
    pmodinit mod_init = (pmodinit)GetProcAddress(GetModuleHandle(nullptr), "imgui_godot_module_init");
#else
    pmodinit mod_init = (pmodinit)dlsym(dlopen(nullptr, RTLD_LAZY), "imgui_godot_module_init");
#endif
    if (mod_init)
    {
        ImGuiMemAllocFunc afunc;
        ImGuiMemFreeFunc ffunc;
        void* ud;
        ImGui::GetAllocatorFunctions(&afunc, &ffunc, &ud);
        mod_init(IMGUI_VERSION_NUM, ImGui::GetCurrentContext(), afunc, ffunc);
    }
}

void initialize_ign_module(ModuleInitializationLevel p_level)
{
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE)
        return;

    ImGui::CreateContext();

    ClassDB::register_internal_class<ImGuiLayer>();
    ClassDB::register_internal_class<ImGuiLayerHelper>();
    ClassDB::register_class<ImGuiGD>();
    ClassDB::register_internal_class<ImGuiWindow>();
    register_imgui_api();

    gd = memnew(ImGuiGD);
    Engine::get_singleton()->register_singleton("ImGuiGD", gd);
    sync_modules();
}

void uninitialize_ign_module(ModuleInitializationLevel p_level)
{
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE)
        return;

    Engine::get_singleton()->unregister_singleton("ImGuiGD");
    memdelete(gd);
}

extern "C" {
GDExtensionBool GDE_EXPORT ign_init(GDExtensionInterfaceGetProcAddress p_get_proc_address,
                                    GDExtensionClassLibraryPtr p_library, GDExtensionInitialization* r_initialization)
{
    GDExtensionBinding::InitObject init_obj(p_get_proc_address, p_library, r_initialization);

    init_obj.register_initializer(initialize_ign_module);
    init_obj.register_terminator(uninitialize_ign_module);
    init_obj.set_minimum_library_initialization_level(MODULE_INITIALIZATION_LEVEL_SCENE);

    return init_obj.init();
}
}
