vcpkg_check_linkage(ONLY_STATIC_LIBRARY)

vcpkg_from_github(
    OUT_SOURCE_PATH SOURCE_PATH
    REPO godotengine/godot-cpp
    REF godot-4.0.3-stable
    SHA512 1a3b0914bcbff481100436da9a522bcaad54f5504042eb468aee2c08557e50cb62402b236437ddb9dc650a714cd7a6f1ac1fcfbf78a9734d88e01c6fe0cef530
    HEAD_REF master
)

vcpkg_cmake_configure(
    SOURCE_PATH "${SOURCE_PATH}"
)
vcpkg_cmake_build()

file(INSTALL "${SOURCE_PATH}/LICENSE.md" DESTINATION "${CURRENT_PACKAGES_DIR}/share/${PORT}" RENAME copyright)

file(INSTALL "${SOURCE_PATH}/include/godot_cpp" DESTINATION "${CURRENT_PACKAGES_DIR}/include")
file(INSTALL "${CURRENT_BUILDTREES_DIR}/${TARGET_TRIPLET}-rel/gen/include/godot_cpp/classes/" DESTINATION "${CURRENT_PACKAGES_DIR}/include/godot_cpp/classes" PATTERN "*.hpp")
file(INSTALL "${CURRENT_BUILDTREES_DIR}/${TARGET_TRIPLET}-rel/gen/include/godot_cpp/variant/" DESTINATION "${CURRENT_PACKAGES_DIR}/include/godot_cpp/variant" PATTERN "*.hpp")
file(INSTALL "${SOURCE_PATH}/gdextension/gdextension_interface.h" DESTINATION "${CURRENT_PACKAGES_DIR}/include")

if(WIN32)
    file(INSTALL "${CURRENT_BUILDTREES_DIR}/${TARGET_TRIPLET}-dbg/bin/godot-cpp.windows.debug.64.lib" DESTINATION "${CURRENT_PACKAGES_DIR}/debug/lib")
    file(INSTALL "${CURRENT_BUILDTREES_DIR}/${TARGET_TRIPLET}-rel/bin/godot-cpp.windows.release.64.lib" DESTINATION "${CURRENT_PACKAGES_DIR}/lib")
elseif(APPLE)
else()
    file(INSTALL "${CURRENT_BUILDTREES_DIR}/${TARGET_TRIPLET}-dbg/bin/libgodot-cpp.linux.debug.64.a" DESTINATION "${CURRENT_PACKAGES_DIR}/debug/lib")
    file(INSTALL "${CURRENT_BUILDTREES_DIR}/${TARGET_TRIPLET}-rel/bin/libgodot-cpp.linux.release.64.a" DESTINATION "${CURRENT_PACKAGES_DIR}/lib")
endif()

vcpkg_copy_pdbs()
