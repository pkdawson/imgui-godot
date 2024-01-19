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
        GIT_TAG 4.2
    )
    FetchContent_MakeAvailable(godot-cpp)
endif()

find_package(freetype CONFIG REQUIRED)
#find_package(unofficial-lunasvg CONFIG REQUIRED)

add_library(imgui-godot-native SHARED)
target_compile_features(imgui-godot-native PRIVATE cxx_std_20)
target_compile_definitions(imgui-godot-native PUBLIC
    IMGUI_USER_CONFIG="imconfig-godot.h"
    IMGUI_ENABLE_FREETYPE
#    IMGUI_ENABLE_FREETYPE_LUNASVG
    IGN_EXPORT
    )

if (MSVC)
    target_compile_options(godot-cpp PRIVATE "/MP")
    target_compile_options(imgui-godot-native PRIVATE "/MP")
endif()

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
    include/imgui-godot.h
    gen/imgui_bindings.gen.h
    gen/cimgui.cpp
    gen/cimgui.h
    )

target_link_libraries(imgui-godot-native PUBLIC
    godot-cpp
    freetype
#    unofficial::lunasvg::lunasvg
    )

target_include_directories(imgui-godot-native PRIVATE src imgui gen PUBLIC include)
set_target_properties(imgui-godot-native PROPERTIES PUBLIC_HEADER "include/imconfig-godot.h;include/imgui-godot.h")

if(WIN32)
    set_property(TARGET imgui-godot-native
        PROPERTY OUTPUT_NAME "libimgui-godot-native.windows.$<IF:$<CONFIG:Debug>,debug,release>.x86_64")
elseif(APPLE)
    set_property(TARGET imgui-godot-native
        PROPERTY OUTPUT_NAME "imgui-godot-native.macos.$<IF:$<CONFIG:Debug>,debug,release>")
    set_target_properties(imgui-godot-native PROPERTIES SUFFIX "")
endif()

set(CMAKE_INSTALL_PREFIX "${CMAKE_SOURCE_DIR}/../addons/imgui-godot")

if(WIN32)
    install(TARGETS imgui-godot-native
        RUNTIME DESTINATION bin
        PUBLIC_HEADER DESTINATION include
        )
elseif(APPLE)
    install(TARGETS imgui-godot-native
        DESTINATION bin/libimgui-godot-native.macos.$<IF:$<CONFIG:Debug>,debug,release>.framework
        PUBLIC_HEADER DESTINATION include
    )
endif()
install(FILES "${CMAKE_SOURCE_DIR}/imgui-godot-native.gdextension"
    DESTINATION .
    )
