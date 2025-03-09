use std::{ffi::CStr, mem::transmute, ptr::null_mut};

use bindings::root::*;
use godot::{engine::ClassDb, prelude::*};

mod bindings;

struct Library;

#[gdextension]
unsafe impl ExtensionLibrary for Library {
    fn on_level_init(level: InitLevel) {
        if level != InitLevel::Scene {
            return;
        }
        
        let obj = ClassDb::singleton().instantiate("ImGuiSync".into());
        
        let ptrs: PackedInt64Array = unsafe {
            obj.call("GetImGuiPtrs", &[
                Variant::from(CStr::from_ptr(ImGui::GetVersion()).to_str().unwrap()),
                Variant::from(size_of::<ImGuiIO>() as i32),
                Variant::from(size_of::<ImDrawVert>() as i32),
                Variant::from(size_of::<ImDrawIdx>() as i32),
                Variant::from(size_of::<ImWchar>() as i32),
            ]).to()
        };
        
        unsafe {
            ImGui::SetCurrentContext(transmute(ptrs[0]));
            ImGui::SetAllocatorFunctions(transmute(ptrs[1]), transmute(ptrs[1]), null_mut());
        }
    }
}

#[derive(GodotClass)]
#[class(base=Node, init)]
struct Example;

#[godot_api]
impl INode for Example {
    fn process(&mut self, _delta: f64) {
        unsafe {
            ImGui::ShowDemoWindow(&mut true);
        }
    }
}
