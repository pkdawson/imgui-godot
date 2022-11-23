cmake_minimum_required(VERSION 3.24)

# set up vcpkg
if (WIN32)
    set(VCPKG_TARGET_TRIPLET x64-windows-static-md)
endif()
file(TO_CMAKE_PATH "$ENV{VCPKG_ROOT}" VCPKG_ROOT)
set(CMAKE_TOOLCHAIN_FILE "${VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake")

project(demoext CXX)

if (NOT TARGET godot-cpp)
    include(FetchContent)
    FetchContent_Declare(
        godot-cpp
        GIT_REPOSITORY https://github.com/godotengine/godot-cpp
        GIT_TAG master # watch out, something will break eventually
    )
    FetchContent_MakeAvailable(godot-cpp)
endif()

find_package(imgui CONFIG REQUIRED)
find_package(implot CONFIG REQUIRED)
find_package(freetype CONFIG REQUIRED)
find_package(GTest CONFIG REQUIRED)

add_library(demoext SHARED)
add_subdirectory(src)

target_link_libraries(demoext PUBLIC
    godot-cpp
    imgui::imgui
    freetype
    implot::implot
    )
target_include_directories(demoext PRIVATE src)
target_compile_features(demoext PRIVATE cxx_std_20)
target_compile_definitions(demoext PUBLIC IMGUI_DISABLE_INCLUDE_IMCONFIG_H)

set_property(TARGET demoext PROPERTY OUTPUT_NAME "demoext.$<IF:$<CONFIG:Debug>,debug,release>")

install(TARGETS demoext RUNTIME DESTINATION bin)

enable_testing()
add_executable(demotest test/test.cpp)
target_include_directories(demotest PRIVATE src)
target_link_libraries(demotest PRIVATE GTest::gtest_main demoext)

include(GoogleTest)
gtest_discover_tests(demotest)
