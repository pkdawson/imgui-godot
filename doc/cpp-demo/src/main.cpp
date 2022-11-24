#include "MyCppNode.h"
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/core/defs.hpp>
#include <godot_cpp/godot.hpp>

using namespace godot;

void initialize_demo_module(ModuleInitializationLevel p_level)
{
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE)
        return;

    ClassDB::register_class<MyCppNode>();
}

void uninitialize_demo_module(ModuleInitializationLevel p_level)
{
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE)
        return;
}

extern "C" {
GDNativeBool GDN_EXPORT demoext_init(const GDNativeInterface* p_interface,
                                     const GDNativeExtensionClassLibraryPtr p_library,
                                     GDNativeInitialization* r_initialization)
{
    godot::GDExtensionBinding::InitObject init_obj(p_interface, p_library, r_initialization);

    init_obj.register_initializer(initialize_demo_module);
    init_obj.register_terminator(uninitialize_demo_module);
    init_obj.set_minimum_library_initialization_level(MODULE_INITIALIZATION_LEVEL_SCENE);

    return init_obj.init();
}
}
