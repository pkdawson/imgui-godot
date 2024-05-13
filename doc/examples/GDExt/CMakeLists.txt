cmake_minimum_required(VERSION 3.26)

# Windows only

set(IMGUI_GODOT_INCLUDE "../../../addons/imgui-godot/include")

project(gdexample CXX)

include(FetchContent)
FetchContent_Declare(
    godot-cpp
    GIT_REPOSITORY https://github.com/godotengine/godot-cpp
    GIT_TAG 4.2
)
FetchContent_MakeAvailable(godot-cpp)

FetchContent_Declare(
    imgui
    GIT_REPOSITORY https://github.com/ocornut/imgui
    GIT_TAG v1.90.5-docking
)
FetchContent_MakeAvailable(imgui)

add_library(gdexample SHARED)
target_compile_features(gdexample PRIVATE cxx_std_20)
target_compile_definitions(gdexample PUBLIC
    IMGUI_USER_CONFIG="imconfig-godot.h"
    )

target_sources(gdexample PRIVATE
    imgui/imgui.cpp
    imgui/imgui_demo.cpp
    imgui/imgui_draw.cpp
    imgui/imgui_tables.cpp
    imgui/imgui_widgets.cpp
    imgui/imgui.h
    src/example.cpp
    src/example.h
    src/register_types.cpp
    src/register_types.h
    )

target_link_libraries(gdexample PUBLIC godot-cpp)

target_include_directories(gdexample PRIVATE src ${IMGUI_GODOT_INCLUDE} imgui)

set_property(TARGET gdexample
    PROPERTY OUTPUT_NAME "libgdexample.windows.template_$<IF:$<CONFIG:Debug>,debug,release>.x86_64")

set(CMAKE_INSTALL_PREFIX "${CMAKE_SOURCE_DIR}/project")

install(TARGETS gdexample
    RUNTIME DESTINATION bin
    )
