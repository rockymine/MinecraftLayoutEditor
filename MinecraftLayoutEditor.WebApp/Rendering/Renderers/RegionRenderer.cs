namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class RegionRenderer : IRenderable, IDisposable
{
    private readonly RegionGeometry _geometry = new();

    public void Render(RenderContext context)
    {
        if (context.MapElement == null)
            return;

        _geometry.Draw(context, context.MapElement.Regions);
    }

    public void Dispose()
    {
        _geometry.Dispose();
        GC.SuppressFinalize(this);
    }
}
