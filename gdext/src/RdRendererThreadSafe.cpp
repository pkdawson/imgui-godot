#include "RdRendererThreadSafe.h"
#include "common.h"
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

    using SharedData = std::pair<RID, std::unique_ptr<ClonedDrawData>>;

    std::mutex sharedDataMutex;
    std::vector<SharedData> dataToDraw;
};

RdRendererThreadSafe::RdRendererThreadSafe() : impl(std::make_unique<Impl>())
{
    RenderingServer* RS = RenderingServer::get_singleton();
    RS->connect("frame_pre_draw", Callable(Engine::get_singleton()->get_singleton("ImGuiLayer"), "on_frame_pre_draw"));
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

        ReplaceTextureRIDs(vp->DrawData);
        RID vprid = make_rid(vp->RendererUserData);
        newData[i].first = GetFramebuffer(vprid);
        newData[i].second = std::make_unique<Impl::ClonedDrawData>(vp->DrawData);
    }

    {
        std::unique_lock<std::mutex> lock(impl->sharedDataMutex);
        impl->dataToDraw = std::move(newData);
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
    for (auto& kv : dataArray)
    {
        if (RD->framebuffer_is_valid(kv.first))
            RdRenderer::Render(kv.first, kv.second->data);
    }

    FreeUnusedTextures();
}

} // namespace ImGui::Godot
