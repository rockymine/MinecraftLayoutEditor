namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class EdgeRenderer : IRenderable
{
    public void Render(RenderContext context)
    {
        context.GraphGeometry.DrawEdges(context);
    }
}
