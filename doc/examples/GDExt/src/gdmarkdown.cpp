#include "gdmarkdown.h"
#include <godot_cpp/classes/atlas_texture.hpp>
#include <godot_cpp/classes/os.hpp>
#include <godot_cpp/classes/resource_loader.hpp>
#include <godot_cpp/classes/texture2d.hpp>
#include <imgui.h>
#include <imgui_markdown.h>

namespace {
using godot::AtlasTexture;
using godot::OS;
using godot::Ref;
using godot::ResourceLoader;
using godot::String;
using godot::Texture2D;

ImGui::MarkdownConfig mdConfig;
ImFont* italicFont = nullptr;

void MdLinkCallback(ImGui::MarkdownLinkCallbackData data)
{
    if (!data.isImage)
    {
        String link(data.link);
        link = link.substr(0, data.linkLength);
        OS::get_singleton()->shell_open(link);
    }
}

ImGui::MarkdownImageData MdImageCallback(ImGui::MarkdownLinkCallbackData data)
{
    ImGui::MarkdownImageData imageData;

    String link(data.link);
    link = link.substr(0, data.linkLength);
    Ref<Texture2D> tex = ResourceLoader::get_singleton()->load(link);

    if (tex.is_valid())
    {
        imageData.isValid = true;
        imageData.useLinkCallback = false;
        imageData.user_texture_id = (ImTextureID)tex->get_rid().get_id();

        if (Ref<AtlasTexture> atex = tex; atex.is_valid())
        {
            imageData.size = atex->get_size();

            Vector2 atlasSize = atex->get_atlas()->get_size();
            imageData.uv0 = atex->get_region().get_position() / atlasSize;
            imageData.uv1 = atex->get_region().get_end() / atlasSize;
        }
        else
        {
            imageData.size = tex->get_size();
        }
    }

    return imageData;
}

void MdFormatCallback(const ImGui::MarkdownFormatInfo& fmt, bool start)
{
    if (fmt.type == ImGui::MarkdownFormatType::EMPHASIS && fmt.level == 1)
    {
        if (start)
            ImGui::PushFont(italicFont);
        else
            ImGui::PopFont();
    }
    else
    {
        ImGui::defaultMarkdownFormatCallback(fmt, start);
    }
}

void MdTooltipCallback(ImGui::MarkdownTooltipCallbackData data)
{
    if (data.linkData.isImage)
    {
        if (data.linkData.textLength > 0)
        {
            ImGui::SetTooltip("%.*s", data.linkData.textLength, data.linkData.text);
        }
    }
    else
    {
        ImGui::SetTooltip("%s Open in browser\n%.*s", data.linkIcon, data.linkData.linkLength, data.linkData.link);
    }
}
} // namespace

void ImGui::InitMarkdown()
{
    auto& io = ImGui::GetIO();
    const auto& fonts = io.Fonts->Fonts;

    mdConfig.linkCallback = MdLinkCallback;
    mdConfig.tooltipCallback = MdTooltipCallback;
    mdConfig.imageCallback = MdImageCallback;
    mdConfig.headingFormats[0] = {fonts[1], true};
    mdConfig.headingFormats[1] = {fonts[2], true};
    mdConfig.headingFormats[2] = {fonts[3], false};
    mdConfig.formatCallback = MdFormatCallback;

    italicFont = fonts[4];
}

void ImGui::Markdown(const std::string& text)
{
    ImGui::Markdown(text.c_str(), text.length(), mdConfig);
}
