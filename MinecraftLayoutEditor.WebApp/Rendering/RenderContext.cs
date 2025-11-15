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
    public MapElement? MapElement { get; }
    public float Scale { get; set; } = 1f;
    public PaintCache Cache { get; }

    public RenderContext(Map map, RenderingOptions options, 
        MapElement mapElement, PaintCache cache )
    {
        Map = map;
        Options = options;
        MapElement = mapElement;
        Cache = cache;
    }

    public void Update(SKSurface surface)
    {
        Surface = surface;
    }
}
