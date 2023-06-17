cmake_minimum_required(VERSION 3.26)

# using cmake for dev on Windows, scons for production builds

# set up vcpkg
if (WIN32)
    set(VCPKG_TARGET_TRIPLET x64-windows-static-md)
endif()
file(TO_CMAKE_PATH "$ENV{VCPKG_ROOT}" VCPKG_ROOT)
set(CMAKE_TOOLCHAIN_FILE "${VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake")

project(imgui-godot-native CXX)

if (NOT TARGET godot-cpp)
    include(FetchContent)
    FetchContent_Declare(
        godot-cpp
        GIT_REPOSITORY https://github.com/godotengine/godot-cpp
        GIT_TAG master # watch out, something will break eventually
    )
    FetchContent_MakeAvailable(godot-cpp)
endif()

find_package(freetype CONFIG REQUIRED)

add_library(imgui-godot-native SHARED)
target_compile_features(imgui-godot-native PRIVATE cxx_std_20)
target_compile_definitions(imgui-godot-native PUBLIC
    IMGUI_USER_CONFIG="imconfig-godot.h"
    IMGUI_ENABLE_FREETYPE
    IGN_EXPORT
    )

add_subdirectory(src)

target_sources(imgui-godot-native PRIVATE
    imgui/imgui.cpp
    imgui/imgui_demo.cpp
    imgui/imgui_draw.cpp
    imgui/imgui_tables.cpp
    imgui/imgui_widgets.cpp
    imgui/imgui.h
    imgui/misc/freetype/imgui_freetype.cpp
    imgui/misc/freetype/imgui_freetype.h
    include/imconfig-godot.h
    include/ImGuiGD.h
    )

target_link_libraries(imgui-godot-native PUBLIC
    godot-cpp
    freetype
    )

target_include_directories(imgui-godot-native PRIVATE src imgui PUBLIC include)
set_target_properties(imgui-godot-native PROPERTIES PUBLIC_HEADER "include/imconfig-godot.h;include/ImGuiGD.h")

set_property(TARGET imgui-godot-native PROPERTY OUTPUT_NAME "libimgui-godot-native.windows.$<IF:$<CONFIG:Debug>,debug,release>.x86_64")

set(CMAKE_INSTALL_PREFIX "${CMAKE_SOURCE_DIR}/proj/addons/imgui-godot-native")
install(TARGETS imgui-godot-native
    RUNTIME DESTINATION bin
    PUBLIC_HEADER DESTINATION include
    )