use std::path::Path;

use bindgen::builder;
use cmd_lib::run_cmd;
use glob::glob;

const IMGUI_GODOT_INCLUDE: &str = "../../../addons/imgui-godot/include";
const IMGUI_TAG: &str = "v1.91.0-docking";

fn main() {
    println!("cargo:rerun-if-changed=godot-cpp");
    println!("cargo:rerun-if-changed=imgui");

    if !Path::new("godot-cpp").exists() {
        run_cmd!(
            git clone --depth 1 -b 4.2 "https://github.com/godotengine/godot-cpp"
        ).unwrap();
    }
    
    if !Path::new("imgui").exists() {
        run_cmd!(
            git clone --depth 1 -b "${IMGUI_TAG}" "https://github.com/ocornut/imgui";
        ).unwrap();
    }
    
    let imgui_src = glob("./imgui/*.cpp").unwrap().map(|p| p.unwrap());
    
    cc::Build::new()
        .cpp(true)
        .static_flag(true)
        
        .include("./imgui")
        .include("./godot-cpp/gdextension")
        .include("./godot-cpp/include")
        .include(IMGUI_GODOT_INCLUDE)
        
        .define("IMGUI_USER_CONFIG", "\"imconfig-godot.h\"")
        
        .files(imgui_src)

        .compile("imgui");
    
    builder()
        .header("./imconfig.h")
        .header("./imgui/imgui.h")
        
        .clang_arg("-xc++")
        .clang_arg("-std=c++11")
        
        .enable_cxx_namespaces()
        
        .generate().unwrap()
        
        .write_to_file(Path::new(&std::env::var("OUT_DIR").unwrap()).join("bindings.rs")).unwrap();
}
