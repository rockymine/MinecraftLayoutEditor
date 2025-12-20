using Microsoft.AspNetCore.Components;
using MinecraftLayoutEditor.XML;

namespace MinecraftLayoutEditor.WebApp.Components
{
    public partial class RegionEditor
    {
        [Parameter]
        public Region Region { get; set; } = default!;
        [Parameter]
        public EventCallback<Region> RegionChanged { get; set; }
        [Parameter]
        public EventCallback SettingsChanged { get; set; }
        [Parameter]
        public EventCallback<Region> OnRegionFocused { get; set; }
        [Parameter]
        public EventCallback OnDelete { get; set; }

        private Task OnRegionChanged(Region updated)
            => RegionChanged.InvokeAsync(updated);

        private Task ReplaceChild(Region parent, Region oldChild, Region newChild)
        {
            // parent is UnionRegion or NegativeRegion
            var list = GetChildrenList(parent);
            int idx = list.IndexOf(oldChild);
            list[idx] = newChild;
            return RegionChanged.InvokeAsync(parent);
        }

        private Task RemoveChild(Region parent, Region child)
        {
            var list = GetChildrenList(parent);
            list.Remove(child);
            return RegionChanged.InvokeAsync(parent);
        }

        private List<Region> GetChildrenList(Region r)
            => r switch
            {
                UnionRegion u => u.Children,
                NegativeRegion n => n.Children,
                _ => throw new InvalidOperationException("Not a composite region")
            };
    }
}