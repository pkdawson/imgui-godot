#include "RdRendererThreadSafe.h"
#include "common.h"
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
            data = (ImDrawData*)IM_ALLOC(sizeof(ImDrawData));
            memcpy(data, drawData, sizeof(ImDrawData));
            data->CmdLists = (ImDrawList**)IM_ALLOC(sizeof(void*) * drawData->CmdListsCount);

            for (int i = 0; i < drawData->CmdListsCount; ++i)
            {
                data->CmdLists[i] = drawData->CmdLists[i]->CloneOutput();
            }
        }

        ~ClonedDrawData()
        {
            for (int i = 0; i < data->CmdListsCount; ++i)
            {
                IM_DELETE(data->CmdLists[i]);
            }
            IM_FREE(data->CmdLists);
            IM_FREE(data);
        }

        ImDrawData* data;
    };

    using SharedData = std::pair<RID, std::unique_ptr<ClonedDrawData>>;

    std::mutex sharedDataMutex;
    std::vector<SharedData> dataToDraw;
};

RdRendererThreadSafe::RdRendererThreadSafe() : impl(std::make_unique<Impl>())
{
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
        // TODO: skip minimized windows
        ImGuiViewport* vp = pio.Viewports[i];
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

    if (dataArray.size() == 0)
        return;

    RenderingDevice* RD = RenderingServer::get_singleton()->get_rendering_device();
    for (auto& kv : dataArray)
    {
        if (RD->framebuffer_is_valid(kv.first))
            RdRenderer::Render(kv.first, kv.second->data);
    }
}

} // namespace ImGui::Godot
