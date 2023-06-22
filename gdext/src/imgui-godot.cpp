#include "imgui-godot.h"
#include "ImGuiGD.h"
#include "Input.h"
#include "RdRenderer.h"
#include "common.h"
#include <imgui.h>

#pragma warning(push, 0)
#include <godot_cpp/classes/image.hpp>
#include <godot_cpp/classes/image_texture.hpp>
#include <godot_cpp/classes/rendering_server.hpp>
#include <godot_cpp/variant/packed_byte_array.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#pragma warning(pop)

using namespace godot;

namespace ImGui::Godot {

namespace {
struct Context
{
    Window* mainWindow = nullptr;
    std::unique_ptr<RdRenderer> renderer;
    std::unique_ptr<Input> input;
    RID svp;
    RID ci;
    Ref<ImageTexture> fontTexture;

    ~Context()
    {
        RenderingServer::get_singleton()->free_rid(ci);
        RenderingServer::get_singleton()->free_rid(svp);
    }
};

std::unique_ptr<Context> ctx;

const char* PlatformName = "godot4";
const char* RendererName = "godot4_rd";
} // namespace

void Init(godot::Window* mainWindow, RID canvasItem, Object* config)
{
    ctx = std::make_unique<Context>();
    ctx->mainWindow = mainWindow;
    ctx->ci = canvasItem;
    ctx->input = std::make_unique<Input>(ctx->mainWindow);

    ImGui::CreateContext();
    ImGuiIO& io = ImGui::GetIO();
    io.DisplaySize = ctx->mainWindow->get_size();

    io.BackendPlatformName = PlatformName;
    io.BackendRendererName = RendererName;

    io.BackendFlags |= ImGuiBackendFlags_HasGamepad;
    io.BackendFlags |= ImGuiBackendFlags_HasMouseCursors;
    io.BackendFlags |= ImGuiBackendFlags_HasSetMousePos;
    io.BackendFlags |= ImGuiBackendFlags_RendererHasVtxOffset;
    //io.BackendFlags |= ImGuiBackendFlags_PlatformHasViewports;
    //io.BackendFlags |= ImGuiBackendFlags_RendererHasViewports;

    if (config)
    {
    }

    ctx->renderer = std::make_unique<RdRenderer>();

    RenderingServer* RS = RenderingServer::get_singleton();
    ctx->svp = RS->viewport_create();
    RS->viewport_set_transparent_background(ctx->svp, true);
    RS->viewport_set_update_mode(ctx->svp, RenderingServer::VIEWPORT_UPDATE_ALWAYS);
    RS->viewport_set_clear_mode(ctx->svp, RenderingServer::VIEWPORT_CLEAR_NEVER);
    RS->viewport_set_active(ctx->svp, true);
    RS->viewport_set_parent_viewport(ctx->svp, ctx->mainWindow->get_viewport_rid());

    // io.Fonts->AddFontFromFileTTF("../../data/Hack-Regular.ttf", 16.0f);
    uint8_t* pixels = nullptr;
    int width = 0, height = 0, bytes_per_pixel = 0;
    io.Fonts->GetTexDataAsRGBA32(&pixels, &width, &height, &bytes_per_pixel);

    PackedByteArray data;
    data.resize(width * height * bytes_per_pixel);
    memcpy(data.ptrw(), pixels, data.size());
    Ref<godot::Image> img = Image::create_from_data(width, height, false, Image::FORMAT_RGBA8, data);
    ctx->fontTexture = ImageTexture::create_from_image(img);
    ImTextureID texid = (ImTextureID)ctx->fontTexture->get_rid().get_id();
    io.Fonts->SetTexID(texid);
    io.Fonts->ClearTexData();
}

void Update(double delta)
{
    ImGuiIO& io = ImGui::GetIO();
    io.DisplaySize = ctx->mainWindow->get_size();
    io.DeltaTime = static_cast<float>(delta);

    ctx->input->Update();

    ImGui::NewFrame();
}

bool ProcessInput(const Ref<InputEvent>& evt, Window* window)
{
    return ctx->input->ProcessInput(evt, window);
}

void ProcessNotification(int what)
{
    return ctx->input->ProcessNotification(what);
}

void Render()
{
    RenderingServer* RS = RenderingServer::get_singleton();
    godot::Vector2i winSize = ctx->mainWindow->get_size();
    RS->viewport_set_size(ctx->svp, winSize.x, winSize.y);
    RID vptex = RS->viewport_get_texture(ctx->svp);
    RS->canvas_item_clear(ctx->ci);
    RS->canvas_item_set_transform(ctx->ci, ctx->mainWindow->get_final_transform().affine_inverse());
    RS->canvas_item_add_texture_rect(ctx->ci, godot::Rect2(0, 0, winSize.x, winSize.y), vptex);

    ImGui::Render();
    ctx->renderer->RenderDrawData(ctx->svp, ImGui::GetDrawData());
}

void Shutdown()
{
    ctx.reset();
    if (ImGui::GetCurrentContext())
        ImGui::DestroyContext();
}

void Connect(const godot::Callable& callable)
{
    Object* igl = Engine::get_singleton()->get_singleton("ImGuiLayer");
    igl->connect("imgui_layout", callable);
}

void ResetFonts()
{
}

void AddFont(FontFile* font_file, int font_size, bool merge)
{
}

void AddFontDefault()
{
}

void RebuildFontAtlas()
{
}

} // namespace ImGui::Godot
