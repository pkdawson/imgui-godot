import json
import os
import sys
import subprocess

sys.path += ["dear_bindings"]
import dear_bindings

# TODO: callbacks as Callable

type_map = {
    "bool": "bool",
    "bool*": "Array",
    "char*": "Array",
    "const char*": "String",
    "const float*": "Array",
    "double": "double",
    "double*": "Array",
    "float": "float",
    "float*": "Array",
    "ImGuiButtonFlags": "BitField<ButtonFlags>",
    "ImGuiCol": "Col",
    "ImGuiColorEditFlags": "BitField<ColorEditFlags>",
    "ImGuiComboFlags": "BitField<ComboFlags>",
    "ImGuiCond": "Cond",
    "ImGuiDataType": "DataType",
    "ImGuiDir": "Dir",
    "ImGuiFocusedFlags": "BitField<FocusedFlags>",
    "ImGuiHoveredFlags": "BitField<HoveredFlags>",
    "ImGuiID": "uint32_t",
    "ImGuiInputTextFlags": "BitField<InputTextFlags>",
    "ImGuiIO*": "Ref<ImGuiIOPtr>",
    "ImGuiKey": "Key",
    "ImGuiMouseButton": "MouseButton",
    "ImGuiPopupFlags": "BitField<PopupFlags>",
    "ImGuiStyleVar": "StyleVar",
    "ImGuiTabBarFlags": "BitField<TabBarFlags>",
    "ImGuiTabItemFlags": "BitField<TabItemFlags>",
    "ImGuiTableColumnFlags": "BitField<TableColumnFlags>",
    "ImGuiTableFlags": "BitField<TableFlags>",
    "ImGuiTableRowFlags": "BitField<TableRowFlags>",
    "ImGuiWindowFlags": "BitField<WindowFlags>",
    "ImTextureID": "Ref<Texture2D>",
    "ImU32": "uint32_t",
    "ImVec2": "Vector2",
    "ImVec4": "Color",
    "int": "int",
    "int*": "Array",
    "size_t": "int64_t",
    "void": "void",
    "ImGuiConfigFlags": "BitField<ConfigFlags>",
    "ImGuiBackendFlags": "BitField<BackendFlags>",
    "ImU16": "uint16_t",
    "ImGuiViewportFlags": "BitField<ViewportFlags>",
    "ImGuiDockNodeFlags": "BitField<DockNodeFlags>",
    "char": "char",
    "ImS16": "int16_t",
    "ImGuiSortDirection": "SortDirection",
    "const ImGuiTableColumnSortSpecs*": "Array",
    "short": "short",
    "ImU8": "uint8_t",
    "ImFont*": "int64_t",
}

# use StringName for these const char* params
sn_names = (
    "desc_id",
    "id",
    "label",
    "name",
    "str_id_begin",
    "str_id_end",
    "str_id",
    "tab_or_docked_window_label",
)

exclude_funcs = (
    "GetKeyIndex",
    "ImGui_DestroyPlatformWindows",
    "ImGui_RenderPlatformWindowsDefault",
    "ImGui_UpdatePlatformWindows",
    "ImGui_NewFrame",
    "ImGui_Render",
    "ImGui_CreateContext",
    "ImGui_DestroyContext",
    "ImGui_GetCurrentContext",
    "ImGui_SetCurrentContext",
    "ImGui_TextUnformatted",  # this is called by Text()
    "ImGui_TextUnformattedEx",
    "ImGui_EndFrame",
)

include_structs = (
    "ImGuiIO",
    # "ImGuiStyle",
    # "ImGuiTableColumnSortSpecs",
    # "ImGuiTableSortSpecs",
    # "ImGuiTextFilter",
    # "ImGuiWindowClass",
)

array_types = {
    "bool*": "bool",
    "char*": "String",
    "int*": "int",
    "float*": "float",
    "double*": "double",
    "const float*": "float",
}


def is_obsolete(j):
    for cond in j.get("conditionals", ()):
        if cond["condition"] == "ifndef" and cond["expression"].find("OBSOLETE") != -1:
            return True
    return False


class Enum:
    def __init__(self, j):
        self.orig_name = j["name"]
        self.obsolete = is_obsolete(j)
        self.bitfield = self.orig_name.endswith("Flags_")
        self.vals = []

        self.name = self.orig_name.strip("_")
        if self.name.startswith("ImGui"):
            self.name = self.name.replace("ImGui", "", 1)

        ignore_endings = tuple(["COUNT", "_", "BEGIN", "END", "OFFSET", "SIZE"])
        for e in j["elements"]:
            name = e["name"]
            if not is_obsolete(e) and not name.endswith(ignore_endings):
                gdname = name.replace("ImGui", "", 1)
                self.vals.append((gdname, name))

    def gen_def(self):
        rv = f"enum {self.name} {{\n"
        for kv in self.vals:
            rv += f"{kv[0]} = {kv[1]},"
        rv += "};\n\n"
        return rv

    def gen_cast(self):
        macro = "VARIANT_BITFIELD_CAST" if self.bitfield else "VARIANT_ENUM_CAST"
        return f"{macro}(ImGui::Godot::{self.name});\n"

    def gen_bindings(self):
        macro = "BIND_BITFIELD_FLAG" if self.bitfield else "BIND_ENUM_CONSTANT"
        rv = ""
        for kv in self.vals:
            rv += f"{macro}({kv[0]}); \\\n"
        return rv


class ReturnType:
    def __init__(self, j):
        self.orig_type = j["declaration"]
        self.gdtype = type_map.get(self.orig_type)
        self.is_struct = self.gdtype and self.gdtype.endswith("Ptr>")


class Param:
    def __init__(self, j):
        self.name = j["name"]
        self.is_array = j["is_array"]
        self.is_varargs = j["is_varargs"]
        if not self.is_varargs:
            self.orig_type = j["type"]["declaration"]
        else:
            self.orig_type = None
        self.gdtype = type_map.get(self.orig_type)
        if self.is_array:
            self.gdtype = "Array"

        if self.gdtype == "String" and self.name in sn_names:
            self.gdtype = "StringName"

        if self.name == "items_separated_by_zeros":
            self.gdtype = "Array"

        dv = j.get("default_value")
        if dv:
            dv = dv.replace("ImVec2", "Vector2")
            dv = dv.replace("ImVec4", "Color")
            dv = dv.replace("FLT_MIN", "std::numeric_limits<float>::min()")
            dv = dv.replace("FLT_MAX", "std::numeric_limits<float>::max()")
            dv = dv.replace("sizeof", "(uint64_t)sizeof")
            if self.gdtype is not None:
                dv = dv.replace("NULL", f"{self.gdtype}()")
        self.dv = dv
        self.is_struct = self.gdtype and self.gdtype.endswith("Ptr>")

    def gen_decl(self):
        rv = f"{self.gdtype} {self.name}"
        # if self.dv:
        #     rv += f" = {self.dv}"
        return rv

    def gen_def(self):
        return f"{self.gdtype} {self.name}"

    def gen_arg(self):
        if self.gdtype == "String":
            if self.dv is not None:
                return f"{self.name}.ptr() ? {self.name}.utf8().get_data() : nullptr"
            else:
                return f"{self.name}.utf8().get_data()"
        elif self.gdtype == "Vector2":
            return f"{{{self.name}.x, {self.name}.y}}"
        elif self.gdtype == "Ref<Texture2D>":
            return f"(ImTextureID){self.name}->get_rid().get_id()"
        elif self.gdtype == "Color":
            return f"{{{self.name}.r, {self.name}.g, {self.name}.b, {self.name}.a}}"
        elif self.gdtype == "StringName":
            return f"sn_to_cstr({self.name})"
        elif self.gdtype == "Array":
            if self.is_array:
                rv = f"({self.orig_type}*)GdsArray<{self.orig_type}>({self.name})"
            elif self.name == "items_separated_by_zeros":
                rv = f"({self.orig_type})GdsZeroArray({self.name})"
            else:
                atype = array_types[self.orig_type]
                rv = f"{self.name}.size() == 0 ? nullptr : " if self.dv else ""
                rv += f"({self.orig_type})GdsPtr<{atype}>({self.name}"
                if atype == "String":
                    rv += ", buf_size, label"
                rv += ")"
            return rv
        elif self.orig_type in ["ImFont*"]:  # opaque pointers
            return f"({self.orig_type}){self.name}"
        elif self.is_struct:
            return f"{self.name}->_GetPtr()"
        else:
            return self.name


class Function:
    def __init__(self, j):
        self.obsolete = is_obsolete(j)
        self.orig_name = j["name"]
        self.name = self.orig_name
        if self.name.startswith("ImGui_"):
            self.name = self.name.replace("ImGui_", "", 1)

        self.rt = ReturnType(j["return_type"])
        self.params = []
        for ja in j["arguments"]:
            self.params.append(Param(ja))
        for p in self.params:
            if p.is_varargs or p.orig_type == "va_list":
                self.params.remove(p)

        self.valid = (
            self.rt.gdtype is not None
            and not self.obsolete
            and self.orig_name not in exclude_funcs
            and not self.orig_name.endswith("V")
            and not self.orig_name.startswith("ImGuiIO_")
            and not self.orig_name.startswith("ImFont_")
        )
        if self.valid:
            for p in self.params:
                if p.gdtype is None:
                    self.valid = False

    def gen_decl(self):
        return f'static {self.rt.gdtype} {self.name}({", ".join(p.gen_decl() for p in self.params)}); \\\n'

    def gen_def(self):
        fname = self.orig_name
        if fname == "ImGui_Text":
            fname = "ImGui_TextUnformatted"  # do your own formatting
        fcall = f'::{fname}({", ".join(p.gen_arg() for p in self.params)})'
        if self.rt.gdtype in ("Vector2", "Color"):
            fcall = f"To{self.rt.gdtype}({fcall})"
        elif self.rt.orig_type in ["ImFont*"]:
            fcall = f"({self.rt.gdtype}){fcall}"

        rv = f'{self.rt.gdtype} ImGui::{self.name}({", ".join(p.gen_def() for p in self.params)}) {{ \\\n'

        if self.rt.is_struct:
            rv += f"{self.rt.gdtype} rv; \\\n"
            rv += "rv.instantiate(); \\\n"
            rv += f"rv->_SetPtr({fcall}); \\\n"
            rv += "return rv"
        else:
            if self.rt.gdtype != "void":
                rv += "return "
            rv += fcall

        rv += "; } \\\n"
        return rv

    def gen_bindings(self):
        rv = f'ClassDB::bind_static_method("ImGui", D_METHOD("{self.name}"'
        for p in self.params:
            rv += f', "{p.name}"'
        rv += f"), &ImGui::{self.name}"
        for p in self.params:
            if p.dv is not None:
                rv += f", DEFVAL({p.dv})"
        rv += "); \\\n"
        return rv


class Property:
    def __init__(self, j, name, struct_name):
        self.struct_name = struct_name
        self.name = name
        self.is_array = j["names"][0].get("is_array", False)
        self.is_internal = j.get("is_internal", False)
        self.orig_type = j["type"]["declaration"]
        self.valid = False
        if self.is_internal:
            return
        self.gdtype = type_map.get(self.orig_type, None)
        if self.is_array:
            self.gdtype = None
        if self.gdtype == "String":
            self.gdtype = None
        self.valid = self.gdtype is not None

    def gen_decl(self):
        rv = f"{self.gdtype} _Get{self.name}(); \\\n"
        rv += f"void _Set{self.name}({self.gdtype} x); \\\n"
        return rv

    def gen_def(self):
        rv = f"{self.gdtype} {self.struct_name}::_Get{self.name}() {{ \\\n"
        fcall = f"ptr->{self.name}"
        # TODO: refactor
        if self.orig_type == "ImVec2":
            fcall = f"ToVector2({fcall})"
        elif self.orig_type in ["ImFont*"]:
            fcall = f"(int64_t){fcall}"

        dv = "{}"
        if self.gdtype.startswith("BitField"):
            dv = "0"
        rv += f"if (ptr) return {fcall}; else return {dv};\\\n"
        rv += "} \\\n"

        rv += f"void {self.struct_name}::_Set{self.name}({self.gdtype} x) {{ \\\n"
        x = "x"
        if self.orig_type == "ImVec2":
            x = "{x.x, x.y}"
        elif self.orig_type in ["ImFont*"]:
            x = "(ImFont*)x"
        rv += f"ptr->{self.name} = {x}; \\\n"
        rv += "} \\\n"
        return rv

    def gen_bindings(self):
        getter = f"_Get{self.name}"
        setter = f"_Set{self.name}"
        rv = f'ClassDB::bind_method(D_METHOD("{getter}"), &{self.struct_name}::{getter}); \\\n'
        rv += f'ClassDB::bind_method(D_METHOD("{setter}", "x"), &{self.struct_name}::{setter}); \\\n'
        if self.gdtype.startswith("BitField<"):
            rv += f'ADD_PROPERTY(PropertyInfo(Variant::INT, "{self.name}"), "{setter}", "{getter}"); \\\n'
        return rv


class Struct:
    def __init__(self, j):
        self.orig_name = j["name"]
        self.name = self.orig_name + "Ptr"
        self.valid = self.orig_name in include_structs
        self.properties = []

        if not self.valid:
            return

        for jfield in j["fields"]:
            for jname in jfield["names"]:
                prop = Property(jfield, jname["name"], self.name)
                if prop.valid:
                    self.properties.append(prop)

    def gen_decl(self):
        rv = f"class {self.name} : public RefCounted {{ \\\n"
        rv += f"GDCLASS({self.name}, RefCounted); \\\n"
        rv += "protected: static void _bind_methods(); \\\n"
        rv += "public: \\\n"
        rv += f"void _SetPtr({self.orig_name}* p) {{ ptr = p; }} \\\n"
        rv += f"{self.orig_name}* _GetPtr() {{ return ptr; }} \\\n"
        for prop in self.properties:
            rv += prop.gen_decl()
        rv += "private: \\\n"
        rv += f"{self.orig_name}* ptr = nullptr; \\\n"
        rv += "}; \\\n"
        return rv

    def gen_def(self):
        rv = f"void {self.name}::_bind_methods() {{ \\\n"
        for prop in self.properties:
            rv += prop.gen_bindings()
        rv += "} \\\n"
        for prop in self.properties:
            rv += prop.gen_def()
        return rv

    def gen_bindings(self):
        return f"ClassDB::register_class<{self.name}>(); \\\n"


class JsonParser:
    array_types = {
        "bool*": "bool",
        "char*": "String",
        "int*": "int",
        "float*": "float",
        "double*": "double",
    }

    def __init__(self):
        for s in include_structs:
            type_map[f"{s}*"] = f"Ref<{s}Ptr>"

        self.enums = []
        self.structs = []
        self.funcs = []
        self.enum_defs = """#include <cimgui.h>

        """
        self.enum_binds = "#define REGISTER_IMGUI_ENUMS() { \\\n"
        self.func_decls = "#define DECLARE_IMGUI_FUNCS() \\\n"
        self.func_binds = "#define BIND_IMGUI_FUNCS() \\\n"
        self.func_defs = "#define DEFINE_IMGUI_FUNCS() \\\n"
        self.struct_decls = "#define DECLARE_IMGUI_STRUCTS() \\\n"
        self.struct_defs = "#define DEFINE_IMGUI_STRUCTS() \\\n"
        self.struct_binds = "#define BIND_IMGUI_STRUCTS() \\\n"

    def write(self):
        try:
            os.mkdir("gen")
        except:
            pass

        with open("gen/imgui_bindings.gen.h", "w") as fi:
            fi.write(self.enum_defs)
            fi.write(self.enum_binds)
            fi.write(self.struct_decls)
            fi.write(self.struct_defs)
            fi.write(self.struct_binds)
            fi.write(self.func_decls)
            fi.write(self.func_binds)
            fi.write(self.func_defs)

    def load(self, jdat):
        self.enum_defs += "namespace ImGui::Godot {\n"
        enums = []
        for je in jdat["enums"]:
            e = Enum(je)
            if e.orig_name.startswith("ImGui"):
                self.enum_defs += e.gen_def()
                self.enum_binds += e.gen_bindings()
                enums.append(e)
        self.enum_defs += "}\n\n"
        self.enum_binds += "}\n"
        for e in enums:
            self.enum_defs += e.gen_cast()

        for js in jdat["structs"]:
            s = Struct(js)
            if s.valid:
                self.structs.append(s)
        for s in self.structs:
            self.struct_decls += s.gen_decl()
            self.struct_defs += s.gen_def()
            self.struct_binds += s.gen_bindings()
        self.struct_decls += "\n\n"
        self.struct_defs += "\n\n"
        self.struct_binds += "\n\n"

        for jf in jdat["functions"]:
            f = Function(jf)
            if f.valid:
                self.funcs.append(f)
            else:
                pass  # print('invalid function ', f.name)
        for f in self.funcs:
            self.func_decls += f.gen_decl()
            self.func_defs += f.gen_def()
            self.func_binds += f.gen_bindings()

        self.func_decls += "\n\n"
        self.func_defs += "\n\n"
        self.func_binds += "\n\n"


def main():
    if not os.path.exists("dear_bindings/cimgui.json"):
        dear_bindings.convert_header(
            "imgui/imgui.h", "dear_bindings/cimgui", "dear_bindings/src/templates"
        )

    parser = JsonParser()
    with open("dear_bindings/cimgui.json") as jfi:
        jdat = json.loads(jfi.read())
        parser.load(jdat)
    parser.write()

    subprocess.call(["clang-format", "-i", "gen/*.h"])


if __name__ == "__main__":
    main()
