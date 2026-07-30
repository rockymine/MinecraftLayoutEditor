namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class NodeRenderer : IRenderable
{
    public void Render(RenderContext context)
    {
        context.GraphGeometry.DrawNodes(context);
    }
}
