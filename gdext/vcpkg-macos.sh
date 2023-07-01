#!/bin/zsh

vcpkg_x64=$(pwd)/vcpkg_installed.x64

$VCPKG_ROOT/vcpkg install --triplet x64-osx
mv vcpkg_installed $vcpkg_x64
$VCPKG_ROOT/vcpkg install --triplet arm64-osx

for libpath in "lib" "debug/lib"
do
    pushd vcpkg_installed/arm64-osx/$libpath
    for x in *.a
    do
        echo "$libpath/$x"
        lipo -create -output $x $x $vcpkg_x64/x64-osx/$libpath/$x
    done
    popd
done

rm -r $vcpkg_x64
