using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.Logic.Geometry;
using MinecraftLayoutEditor.XML;
using SkiaSharp;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class RenderContext
{
    public SKSurface? Surface { get; set; }
    public Map Map { get; }
    public RenderingOptions Options { get; }
    public Node? SelectedNode { get; set; }
    public Node? HoveredNode { get; set; }
    public Region? HoveredRegion { get; set; }
    public MapElement MapElement { get; private set; } = null!;
    public PaintCache Cache { get; }
    public Viewport Viewport { get; set; }
    public RegionType SelectedRegionType { get; set; } = RegionType.Circle;

    public int LimitX => (int)(Map.Width / 2f);
    public int LimitY => (int)(Map.Height / 2f);

    public RenderContext(Map map, RenderingOptions options, 
        Viewport viewport, PaintCache cache )
    {
        Map = map;
        Options = options;
        Cache = cache;
        Viewport = viewport;
    }

    public void RegisterSurface(SKSurface surface)
    {
        Surface = surface;
    }

    public void RegisterMapElement(MapElement mapElement)
    {
        MapElement = mapElement ?? throw new ArgumentNullException(nameof(mapElement));
    }

    public void EnsureMapElement()
    {
        MapElement ??= new MapElement()
        {
            Proto = "1.5.0",
            Name = "Untitled Map",
            Version = "1.0.0",
            Gamemode = "ctw",
            MaxBuildHeight = 100
        };
    }

    public Region? GetRegionContaining(Vector2 position)
    {
        if (MapElement == null || MapElement.Regions.Items.Count == 0)
            return null;

        for (int i = MapElement.Regions.Items.Count - 1; i >= 0; i--)
        {
            var region = MapElement.Regions.Items[i];
            if (region.Contains(position))
                return region;
        }

        return null;
    }
}
