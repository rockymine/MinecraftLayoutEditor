using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class MapRenderer : IDisposable
{
    public RenderContext RenderContext { get; set; }
    public List<IRenderable> renderables = [];

    public MapRenderer(RenderContext renderContext)
    {
        RenderContext = renderContext;
        RenderContext.Viewport.UpdateTRS(new Vector2(25, 25));
    }

    public void Render()
    {
        var profiler = RenderContext.Profiler;
        profiler.BeginFrame();

        RenderContext.Surface!.Canvas.SetMatrix(RenderContext.Viewport.SKWorldToScreen);

        foreach (var renderable in renderables)
        {
            profiler.BeginRenderer(renderable.GetType().Name);
            renderable.Render(RenderContext);
            profiler.EndRenderer();
        }

        profiler.EndFrame();
    }

    public void Dispose()
    {
        foreach (var renderable in renderables.OfType<IDisposable>())
            renderable.Dispose();

        GC.SuppressFinalize(this);
    }
}
