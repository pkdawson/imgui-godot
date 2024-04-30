#include "register_types.h"
#include "core/object/class_db.h"
#include "mynode.h"
#include <imgui-godot.h>

// use this macro once in any module
IMGUI_GODOT_MODULE_INIT()

void initialize_mymod_module(ModuleInitializationLevel p_level)
{
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE)
        return;
    ClassDB::register_class<MyNode>();
}

void uninitialize_mymod_module(ModuleInitializationLevel p_level)
{
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE)
        return;
}
