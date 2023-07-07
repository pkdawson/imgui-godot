#include "Fonts.h"

#include <godot_cpp/classes/image.hpp>
#include <godot_cpp/classes/image_texture.hpp>
#include <godot_cpp/classes/texture2d.hpp>
#include <imgui.h>

#include <filesystem>
#include <vector>

using namespace godot;
using namespace std::literals;
namespace fs = std::filesystem;

namespace ImGui::Godot {

struct Fonts::Impl
{
    Ref<Texture2D> fontTexture;

    struct FontParams
    {
        Ref<FontFile> font;
        int fontSize;
        bool merge;
        ImVector<ImWchar> ranges;
    };
    std::vector<FontParams> fontConfig;

    void AddFontToAtlas(const Ref<FontFile>& font, int fontSize, bool merge, const ImVector<ImWchar>& ranges);

    static ImVector<ImWchar> GetRanges(const Ref<Font>& font)
    {
        ImVector<ImWchar> rv;
        if (!font.is_null())
        {
            ImFontGlyphRangesBuilder builder;
            builder.AddText(font->get_supported_chars().utf8().get_data());
            builder.BuildRanges(&rv);
        }
        return rv;
    }

    static void ResetStyle()
    {
        ImGuiStyle defaultStyle;
        ImGuiStyle& style = ImGui::GetStyle();

        style.WindowPadding = defaultStyle.WindowPadding;
        style.WindowRounding = defaultStyle.WindowRounding;
        style.WindowMinSize = defaultStyle.WindowMinSize;
        style.ChildRounding = defaultStyle.ChildRounding;
        style.PopupRounding = defaultStyle.PopupRounding;
        style.FramePadding = defaultStyle.FramePadding;
        style.FrameRounding = defaultStyle.FrameRounding;
        style.ItemSpacing = defaultStyle.ItemSpacing;
        style.ItemInnerSpacing = defaultStyle.ItemInnerSpacing;
        style.CellPadding = defaultStyle.CellPadding;
        style.TouchExtraPadding = defaultStyle.TouchExtraPadding;
        style.IndentSpacing = defaultStyle.IndentSpacing;
        style.ColumnsMinSpacing = defaultStyle.ColumnsMinSpacing;
        style.ScrollbarSize = defaultStyle.ScrollbarSize;
        style.ScrollbarRounding = defaultStyle.ScrollbarRounding;
        style.GrabMinSize = defaultStyle.GrabMinSize;
        style.GrabRounding = defaultStyle.GrabRounding;
        style.LogSliderDeadzone = defaultStyle.LogSliderDeadzone;
        style.TabRounding = defaultStyle.TabRounding;
        style.TabMinWidthForCloseButton = defaultStyle.TabMinWidthForCloseButton;
        style.DisplayWindowPadding = defaultStyle.DisplayWindowPadding;
        style.DisplaySafeAreaPadding = defaultStyle.DisplaySafeAreaPadding;
        style.MouseCursorScale = defaultStyle.MouseCursorScale;
    }
};

void Fonts::Impl::AddFontToAtlas(const Ref<FontFile>& font, int fontSize, bool merge, const ImVector<ImWchar>& ranges)
{
    ImFontConfig fc;
    if (merge)
        fc.MergeMode = 1;

    if (font.is_null())
    {
        // default font
        fc = {};
        fc.SizePixels = fontSize;
        fc.OversampleH = 1;
        fc.OversampleV = 1;
        fc.PixelSnapH = true;
        ImGui::GetIO().Fonts->AddFontDefault(&fc);
    }
    else
    {
        fs::path fontpath = (font->get_path().utf8().get_data());

        // no std::format in Clang 14
        std::string fontdesc = fontpath.filename().string() + ", "s + std::to_string(fontSize) + "px";
        if (fontdesc.length() > 39)
            fontdesc.resize(39);
        std::copy(fontdesc.begin(), fontdesc.end(), fc.Name);

        int64_t len = font->get_data().size();
        // let ImGui manage this memory
        void* p = ImGui::MemAlloc(len);
        memcpy(p, font->get_data().ptr(), len);
        ImGui::GetIO().Fonts->AddFontFromMemoryTTF(p, len, fontSize, &fc, ranges.Data);
    }

    if (merge)
    {
        ImGui::GetIO().Fonts->Build();
    }
}

Fonts::Fonts() : impl(std::make_unique<Impl>())
{
}

Fonts::~Fonts()
{
}

void Fonts::Reset()
{
    ImGuiIO& io = ImGui::GetIO();
    io.Fonts->Clear();
    io.FontDefault = nullptr;
    impl->fontConfig.clear();
}

void Fonts::Add(Ref<FontFile> fontData, int fontSize, bool merge)
{
    impl->fontConfig.push_back({fontData, fontSize, merge, Impl::GetRanges(fontData)});
}

void Fonts::RebuildFontAtlas(float scale)
{
    ImGuiIO& io = ImGui::GetIO();
    int fontIndex = -1;
    if (io.FontDefault != nullptr)
    {
        for (int i = 0; i < io.Fonts->Fonts.Size; ++i)
        {
            if (io.Fonts->Fonts[i] == io.FontDefault)
            {
                fontIndex = i;
                break;
            }
        }
        io.FontDefault = nullptr;
    }
    io.Fonts->Clear();

    for (const auto& fp : impl->fontConfig)
    {
        impl->AddFontToAtlas(fp.font, fp.fontSize * scale, fp.merge, fp.ranges);
    }

    uint8_t* pixelData;
    int width;
    int height;
    int bytesPerPixel;
    io.Fonts->GetTexDataAsRGBA32(&pixelData, &width, &height, &bytesPerPixel);

    PackedByteArray pixels;
    pixels.resize(width * height * bytesPerPixel);
    memcpy(pixels.ptrw(), pixelData, pixels.size());

    Ref<godot::Image> img = Image::create_from_data(width, height, false, Image::FORMAT_RGBA8, pixels);
    Ref<ImageTexture> imgtex = ImageTexture::create_from_image(img);
    impl->fontTexture = imgtex;

    io.Fonts->SetTexID((ImTextureID)impl->fontTexture->get_rid().get_id());
    io.Fonts->ClearTexData();

    if (fontIndex != -1 && fontIndex < io.Fonts->Fonts.Size)
    {
        io.FontDefault = io.Fonts->Fonts[fontIndex];
    }

    Impl::ResetStyle();
    ImGui::GetStyle().ScaleAllSizes(scale);
}

} // namespace ImGui::Godot
