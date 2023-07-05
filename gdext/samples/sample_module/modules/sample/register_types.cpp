#include "register_types.h"
#include "core/object/class_db.h"
#include "sample_node.h"

// put this in only one .cpp file in one module
#include "imgui-godot.h"
IMGUI_GODOT_MODULE_INIT()

void initialize_sample_module(ModuleInitializationLevel p_level)
{
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE)
        return;

    ClassDB::register_class<SampleNode>();
}

void uninitialize_sample_module(ModuleInitializationLevel p_level)
{
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE)
        return;
}
