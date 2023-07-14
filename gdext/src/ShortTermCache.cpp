#include "ShortTermCache.h"
#include <godot_cpp/variant/utility_functions.hpp>
#include <imgui.h>
#include <iterator>
#include <string_view>
#include <unordered_map>

namespace ImGui::Godot {

std::unique_ptr<ShortTermCache> gdscache = std::make_unique<ShortTermCache>();

struct ShortTermCache::Impl
{
    std::unordered_map<int64_t, std::vector<char>> bufs;
    std::unordered_map<int64_t, bool> used;

    static void CopyInput(std::vector<char>& buf, const Array& a)
    {
        CharString cs = String(a[0]).utf8();
        std::string_view sv = cs.get_data();
        if (sv.size() >= buf.size())
            return;
        std::copy(sv.begin(), sv.end(), buf.begin());
        buf[sv.size()] = '\0';
    }
};

ShortTermCache::ShortTermCache() : impl(std::make_unique<Impl>())
{
}

ShortTermCache::~ShortTermCache()
{
}

void ShortTermCache::OnNewFrame()
{
    for (auto it = impl->used.begin(); it != impl->used.end();)
    {
        if (!it->second)
        {
            impl->bufs.erase(it->first);
            it = impl->used.erase(it);
        }
        else
        {
            it->second = false;
            ++it;
        }
    }
}

std::vector<char>& ShortTermCache::GetTextBuf(const StringName& label, size_t size, const Array& a)
{
    int64_t hash = ImGui::GetID((void*)label.hash());
    impl->used[hash] = true;
    auto it = impl->bufs.find(hash);
    if (it == impl->bufs.end())
    {
        impl->bufs[hash] = std::vector<char>(size);
        std::vector<char>& buf = impl->bufs[hash];
        Impl::CopyInput(buf, a);
        return buf;
    }
    else
    {
        std::vector<char>& buf = it->second;
        buf.resize(size);
        Impl::CopyInput(buf, a);
        return buf;
    }
}

const std::vector<char>& ShortTermCache::GetZeroArray(const Array& a)
{
    int64_t hash = a.hash();
    impl->used[hash] = true;
    if (auto it = impl->bufs.find(hash); it != impl->bufs.end())
    {
        return it->second;
    }

    impl->bufs[hash] = {};
    std::vector<char>& buf = impl->bufs[hash];
    for (int i = 0; i < a.size(); ++i)
    {
        CharString cs = String(a[i]).utf8();
        std::string_view sv = cs.get_data();
        std::copy(sv.begin(), sv.end(), std::back_inserter(buf));
        buf.push_back('\0');
    }
    buf.push_back('\0');
    return buf;
}

} // namespace ImGui::Godot
