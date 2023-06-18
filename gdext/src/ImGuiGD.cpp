#include "ImGuiGD.h"
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
using ImGui::Godot::RdRenderer;

namespace {

struct Context
{
    Window* mainWindow = nullptr;
    std::unique_ptr<RdRenderer> renderer;
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
} // namespace

namespace ImGui::Godot {
void Init(godot::Window* mainWindow, RID canvasItem)
{
    ctx = std::make_unique<Context>();
    ctx->mainWindow = mainWindow;
    ctx->ci = canvasItem;

    ImGui::CreateContext();
    ImGuiIO& io = ImGui::GetIO();
    io.DisplaySize = ctx->mainWindow->get_size();

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

    ImGui::NewFrame();
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

void Connect(godot::Callable& callable)
{
}
} // namespace ImGui::Godot

void IGN_API ImGuiGodot_Init(godot::Window* mainWindow, int64_t canvasItemRID)
{
    ImGui::Godot::Init(mainWindow, ImGui::Godot::make_rid(canvasItemRID));
}

void IGN_API ImGuiGodot_Update(double delta)
{
    ImGui::Godot::Update(delta);
}

void IGN_API ImGuiGodot_Render()
{
    ImGui::Godot::Render();
}

void IGN_API ImGuiGodot_Shutdown()
{
    ImGui::Godot::Shutdown();
}

void IGN_API ImGuiGodot_Connect(godot::Callable* callable)
{
    ImGui::Godot::Connect(*callable);
}
