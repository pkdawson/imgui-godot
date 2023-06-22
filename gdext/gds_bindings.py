import json
import os
import sys
import subprocess

sys.path += ['dear_bindings']
import dear_bindings

class JsonParser:
    type_map = {
        'const char*' : 'String',
        'ImGuiID' : 'uint32_t',
        'ImVec2' : 'Vector2',
        'ImVec4' : 'Color',
        'void' : 'void',
        'bool' : 'bool',
        'float' : 'float',
        'ImU32' : 'uint32_t',
        'int' : 'int',
        'double' : 'double',
        'size_t' : 'int64_t',
        'ImGuiWindowFlags' : 'BitField<WindowFlags>',
        'ImGuiFocusedFlags' : 'BitField<FocusedFlags>',
        'ImGuiHoveredFlags' : 'BitField<HoveredFlags>',
        'ImGuiCol' : 'Col',
        'ImTextureID' : 'Ref<Texture2D>',
        'ImGuiMouseButton' : 'MouseButton',
        'ImGuiKey' : 'Key',
        'ImGuiPopupFlags' : 'BitField<PopupFlags>',
        'ImGuiTableFlags' : 'BitField<TableFlags>',
        'ImGuiCond' : 'Cond',
    }

    def __init__(self):
        self.enum_list = []
        self.enums_h = '''#include <cimgui.h>

        '''
        self.enums_c = '#define REGISTER_IMGUI_ENUMS() { \\\n'
        self.func_decls = '#define DECLARE_IMGUI_FUNCS() \\\n'
        self.func_binds = '#define BIND_IMGUI_FUNCS() \\\n'
        self.func_defs = '#define DEFINE_IMGUI_FUNCS() \\\n'
    
    def write(self):
        try:
            os.mkdir('gen')
        except:
            pass
    
        with open('gen/imgui_enums.gen.h', 'w') as fi:
            fi.write(self.enums_h)
            fi.write(self.enums_c)
    
        with open('gen/imgui_funcs.gen.h', 'w') as fi:
            fi.write(self.func_decls)
            fi.write(self.func_binds)
            fi.write(self.func_defs)

    def load(self, jdat):
        self.enums_h += 'namespace ImGui::Godot {\n'
        for e in jdat['enums']:
            self.handle_enum(e)
        self.enums_h += '}\n'
        for name in self.enum_list:
            macro = 'VARIANT_BITFIELD_CAST' if name.endswith('Flags') else 'VARIANT_ENUM_CAST'
            self.enums_h += f'{macro}(ImGui::Godot::{name});\n\n'
        self.enums_c += '}\n'

        for f in jdat['functions']:
           self.handle_function(f)
        self.func_decls += '\n\n'
        self.func_binds += '\n\n'
        self.func_defs += '\n'
        
    def handle_enum(self, j):
        for cond in j.get('conditionals', ()):
            if cond['condition'] == 'ifndef' and cond['expression'] == 'IMGUI_DISABLE_OBSOLETE_KEYIO':
                return

        ename = j['name']
        if not ename.startswith('ImGui'):
            return
        shortname = ename[5:]
        if shortname.endswith('_'):
            shortname = shortname[:-1]
        self.enums_h += f'enum {shortname} {{\n'
        macro = 'BIND_BITFIELD_FLAG' if shortname.endswith('Flags') else 'BIND_ENUM_CONSTANT'
        for e in j['elements']:
            name = e['name']
            skip = False
            for cond in e.get('conditionals', ()):
                if cond['condition'] == 'ifndef' and cond['expression'] == 'IMGUI_DISABLE_OBSOLETE_KEYIO':
                    skip = True
            if skip:
                continue

            if not name.endswith('COUNT') \
                and not name.endswith('_') \
                and not name.endswith('BEGIN') \
                and not name.endswith('END') \
                and not name.endswith('OFFSET') \
                and not name.endswith('SIZE'):
                gdname = name.replace("ImGui", "", 1)
                self.enums_h += f'{gdname} = {name},\n'
                self.enums_c += f'{macro}({gdname}); \\\n'
                
        self.enums_h += '};\n'
        self.enum_list.append(shortname)
            
    def handle_function(self, j):
        name = j['name']
        if name.startswith('ImGui_'):
            name = name.replace('ImGui_', '', 1)
            rt = j['return_type']['declaration']
            rtv = self.type_map.get(rt)
            if rtv is None:
                return

            for cond in j.get('conditionals', ()):
                if cond['condition'] == 'ifndef' and cond['expression'] == 'IMGUI_DISABLE_OBSOLETE_FUNCTIONS':
                    return

            tmp_bind = f'ClassDB::bind_static_method("ImGui", D_METHOD("{name}"'
            argsout = []
            argnames = []
            argtypes = []
            defvals = []
            for arg in j['arguments']:
                if arg.get('type') is not None:
                    an = arg['name']
                    at = arg['type']['declaration']
                    atv = self.type_map.get(at)
                    if atv is None or arg['is_array']:
                        return
                    argsout.append(f'{atv} {an}')
                    argnames.append(an)
                    argtypes.append(atv)
                    if arg.get('default_value'):
                        dv = arg['default_value']
                        dv = dv.replace('ImVec2', 'Vector2')
                        dv = dv.replace('ImVec4', 'Color')
                        defvals.append(dv)

            for n in argnames:
                tmp_bind += f', "{n}"'
            tmp_bind += f'), &ImGui::{name}'
            for dv in defvals:
                tmp_bind += f', DEFVAL({dv})'
            tmp_bind += ');'

            call_args = []
            for i in range(len(argnames)):
                an = argnames[i]
                at = argtypes[i]
                if at == 'String':
                    call_args.append(f'{an}.utf8().get_data()')
                elif at == 'Vector2':
                    call_args.append(f'{{{an}.x, {an}.y}}')
                elif at == 'Ref<Texture2D>':
                    call_args.append(f'(ImTextureID){an}->get_rid().get_id()')
                elif at == 'Color':
                    call_args.append(f'{{{an}.r, {an}.g, {an}.b, {an}.a}}')
                else:
                    call_args.append(an)

            convert_rts = ('Vector2', 'Color')

            self.func_decls += f'static {rtv} {name}({", ".join(argsout)}); \\\n'
            self.func_binds += tmp_bind + " \\\n"

            self.func_defs += f'{rtv} ImGui::{name}({", ".join(argsout)}) {{ \\\n'
            if rtv != 'void':
                self.func_defs += "return "
            if rtv in convert_rts:
                self.func_defs += f'To{rtv}('
            self.func_defs += f'{j["name"]}({", ".join(call_args)}){")" if rtv in convert_rts else ""}; }} \\\n'

def main():
    if not os.path.exists('dear_bindings/cimgui.json'):
        dear_bindings.convert_header('imgui/imgui.h', 'dear_bindings/cimgui', 'dear_bindings/src/templates')

    parser = JsonParser()
    with open('dear_bindings/cimgui.json') as jfi:
        jdat = json.loads(jfi.read())
        parser.load(jdat)
    parser.write()

    subprocess.call('clang-format -i gen/*.h')
    #subprocess.call('clang-format -i gen/*.cpp')

if __name__ == '__main__':
    main()
