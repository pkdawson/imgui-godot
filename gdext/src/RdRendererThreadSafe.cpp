#include "RdRendererThreadSafe.h"
#include "common.h"
#include <godot_cpp/classes/display_server.hpp>
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/rendering_server.hpp>
#include <mutex>

using namespace godot;

namespace ImGui::Godot {

struct RdRendererThreadSafe::Impl
{
    struct ClonedDrawData
    {
        ClonedDrawData(ImDrawData* drawData)
        {
            static_assert(sizeof(ImDrawData) == 64);
            data = IM_NEW(ImDrawData);
            data->Valid = drawData->Valid;
            data->CmdListsCount = drawData->CmdListsCount;
            data->TotalIdxCount = drawData->TotalIdxCount;
            data->TotalVtxCount = drawData->TotalVtxCount;
            data->CmdLists = {};
            data->DisplayPos = drawData->DisplayPos;
            data->DisplaySize = drawData->DisplaySize;
            data->FramebufferScale = drawData->FramebufferScale;
            data->OwnerViewport = drawData->OwnerViewport;

            for (int i = 0; i < drawData->CmdLists.Size; ++i)
            {
                data->CmdLists.push_back(drawData->CmdLists[i]->CloneOutput());
            }
        }

        ~ClonedDrawData()
        {
            for (int i = 0; i < data->CmdLists.Size; ++i)
            {
                IM_DELETE(data->CmdLists[i]);
            }
            IM_DELETE(data);
        }

        ImDrawData* data;
    };

    bool isGodot42 = false;

    using SharedData = std::pair<RID, std::unique_ptr<ClonedDrawData>>;

    // mutex overhead should be minimal (near-zero contention), and refactoring to atomic is ugly
    std::mutex sharedDataMutex;
    std::vector<SharedData> dataToDraw;
};

RdRendererThreadSafe::RdRendererThreadSafe() : impl(std::make_unique<Impl>())
{
    // TODO: remove 4.2 compatibility when 4.4 is released
    Dictionary vinfo = Engine::get_singleton()->get_version_info();
    impl->isGodot42 = (int)vinfo["hex"] < 0x040300;

    if (impl->isGodot42)
    {
        RenderingServer* RS = RenderingServer::get_singleton();
        RS->connect("frame_pre_draw",
                    Callable(Engine::get_singleton()->get_singleton("ImGuiController"), "on_frame_pre_draw"));
    }

    if (DisplayServer::get_singleton()->window_get_vsync_mode() == DisplayServer::VSYNC_DISABLED)
    {
        UtilityFunctions::push_warning(
            "[imgui-godot] Multi-threaded renderer with vsync disabled will probably crash");
    }
}

RdRendererThreadSafe::~RdRendererThreadSafe()
{
}

void RdRendererThreadSafe::Render()
{
    auto& pio = ImGui::GetPlatformIO();
    std::vector<Impl::SharedData> newData(pio.Viewports.size());

    for (int i = 0; i < pio.Viewports.size(); ++i)
    {
        ImGuiViewport* vp = pio.Viewports[i];
        if (vp->Flags & ImGuiViewportFlags_IsMinimized)
            continue;

        RID vprid = make_rid(vp->RendererUserData);
        if (impl->isGodot42)
        {
            ReplaceTextureRIDs(vp->DrawData);
            newData[i].first = GetFramebuffer(vprid);
        }
        else
        {
            newData[i].first = vprid;
        }
        newData[i].second = std::make_unique<Impl::ClonedDrawData>(vp->DrawData);
    }

    {
        std::unique_lock<std::mutex> lock(impl->sharedDataMutex);
        impl->dataToDraw = std::move(newData);
    }

    if (!impl->isGodot42)
    {
        RenderingServer::get_singleton()->call_on_render_thread(
            Callable(Engine::get_singleton()->get_singleton("ImGuiController"), "on_frame_pre_draw"));
    }
}

void RdRendererThreadSafe::OnFramePreDraw()
{
    std::vector<Impl::SharedData> dataArray;

    {
        std::unique_lock<std::mutex> lock(impl->sharedDataMutex);
        // take ownership of shared data
        dataArray = std::move(impl->dataToDraw);
    }

    RenderingDevice* RD = RenderingServer::get_singleton()->get_rendering_device();
    for (const auto& kv : dataArray)
    {
        RID fb = kv.first;
        ImDrawData* drawData = kv.second->data;

        if (!impl->isGodot42)
        {
            fb = GetFramebuffer(fb);
            ReplaceTextureRIDs(drawData);
        }

        if (RD->framebuffer_is_valid(fb))
        {
            RdRenderer::Render(fb, drawData);
        }
    }

    FreeUnusedTextures();
}

} // namespace ImGui::Godot
