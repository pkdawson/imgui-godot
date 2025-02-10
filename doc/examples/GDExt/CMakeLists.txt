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
    GIT_TAG v1.91.6-docking
)
FetchContent_MakeAvailable(imgui)

FetchContent_Declare(
    implot
    GIT_REPOSITORY https://github.com/epezent/implot
    GIT_TAG master
)
FetchContent_MakeAvailable(implot)

FetchContent_Declare(
    imgui_markdown
    GIT_REPOSITORY https://github.com/enkisoftware/imgui_markdown
    GIT_TAG main
)
FetchContent_MakeAvailable(imgui_markdown)

add_library(gdexample SHARED)
target_compile_features(gdexample PRIVATE cxx_std_20)
target_compile_definitions(gdexample PUBLIC
    IMGUI_USER_CONFIG="imconfig-godot.h"
)
if (MSVC)
    target_compile_options(godot-cpp PRIVATE "/MP")
    target_compile_options(gdexample PRIVATE "/MP")
    set(CMAKE_VS_INCLUDE_INSTALL_TO_DEFAULT_BUILD ON)
endif()

target_sources(gdexample PRIVATE
    ${imgui_SOURCE_DIR}/imgui.cpp
    ${imgui_SOURCE_DIR}/imgui_demo.cpp
    ${imgui_SOURCE_DIR}/imgui_draw.cpp
    ${imgui_SOURCE_DIR}/imgui_tables.cpp
    ${imgui_SOURCE_DIR}/imgui_widgets.cpp
    ${imgui_SOURCE_DIR}/imgui.h
    ${implot_SOURCE_DIR}/implot.cpp
    ${implot_SOURCE_DIR}/implot_demo.cpp
    ${implot_SOURCE_DIR}/implot_items.cpp
    ${implot_SOURCE_DIR}/implot.h
    ${imgui_markdown_SOURCE_DIR}/imgui_markdown.h
    src/example.cpp
    src/example.h
    src/register_types.cpp
    src/register_types.h
    src/gdmarkdown.cpp
    src/gdmarkdown.h
)

target_link_libraries(gdexample PUBLIC godot-cpp)

target_include_directories(gdexample PRIVATE
    src
    ${IMGUI_GODOT_INCLUDE}
    ${imgui_SOURCE_DIR}
    ${implot_SOURCE_DIR}
    ${imgui_markdown_SOURCE_DIR}
)

set_property(TARGET gdexample
    PROPERTY OUTPUT_NAME "libgdexample.windows.template_$<IF:$<CONFIG:Debug>,debug,release>.x86_64")

set(CMAKE_INSTALL_PREFIX "${CMAKE_SOURCE_DIR}/project")

install(TARGETS gdexample
    RUNTIME DESTINATION bin
)
