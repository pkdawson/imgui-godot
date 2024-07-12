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
    GIT_TAG v1.90.9-docking
)
FetchContent_MakeAvailable(imgui)

FetchContent_Declare(
    implot
    GIT_REPOSITORY https://github.com/epezent/implot
    GIT_TAG master
)
FetchContent_MakeAvailable(implot)

add_library(gdexample SHARED)
target_compile_features(gdexample PRIVATE cxx_std_20)
target_compile_definitions(gdexample PUBLIC
    IMGUI_USER_CONFIG="imconfig-godot.h"
    )

target_sources(gdexample PRIVATE
    ${FETCHCONTENT_BASE_DIR}/imgui-src/imgui.cpp
    ${FETCHCONTENT_BASE_DIR}/imgui-src/imgui_demo.cpp
    ${FETCHCONTENT_BASE_DIR}/imgui-src/imgui_draw.cpp
    ${FETCHCONTENT_BASE_DIR}/imgui-src/imgui_tables.cpp
    ${FETCHCONTENT_BASE_DIR}/imgui-src/imgui_widgets.cpp
    ${FETCHCONTENT_BASE_DIR}/imgui-src/imgui.h
    ${FETCHCONTENT_BASE_DIR}/implot-src/implot.cpp
    ${FETCHCONTENT_BASE_DIR}/implot-src/implot_demo.cpp
    ${FETCHCONTENT_BASE_DIR}/implot-src/implot_items.cpp
    ${FETCHCONTENT_BASE_DIR}/implot-src/implot.h
    src/example.cpp
    src/example.h
    src/register_types.cpp
    src/register_types.h
    )

target_link_libraries(gdexample PUBLIC godot-cpp)

target_include_directories(gdexample PRIVATE
    src
    ${IMGUI_GODOT_INCLUDE}
    ${FETCHCONTENT_BASE_DIR}/imgui-src
    ${FETCHCONTENT_BASE_DIR}/implot-src
    )

set_property(TARGET gdexample
    PROPERTY OUTPUT_NAME "libgdexample.windows.template_$<IF:$<CONFIG:Debug>,debug,release>.x86_64")

set(CMAKE_INSTALL_PREFIX "${CMAKE_SOURCE_DIR}/project")

install(TARGETS gdexample
    RUNTIME DESTINATION bin
    )
