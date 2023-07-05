#include "register_types.h"
#include "another_node.h"
#include "core/object/class_db.h"

void initialize_another_module(ModuleInitializationLevel p_level)
{
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE)
        return;

    ClassDB::register_class<AnotherNode>();
}

void uninitialize_another_module(ModuleInitializationLevel p_level)
{
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE)
        return;
}
