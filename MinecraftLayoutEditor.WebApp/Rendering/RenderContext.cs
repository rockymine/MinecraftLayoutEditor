using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.XML;
using SkiaSharp;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class RenderContext
{
    public SKSurface? Surface { get; set; }
    public Map Map { get; }
    public RenderingOptions Options { get; }
    public Node? SelectedNode { get; set; }
    public Node? HoveredNode { get; set; }
    public MapElement? MapElement { get; set; }
    public PaintCache Cache { get; }
    public Viewport Viewport { get; set; }

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
        MapElement = mapElement;
    }
}
