namespace MinecraftLayoutEditor.WebApp.Rendering.Renderers;

public class EdgeBoundingBoxRenderer : IRenderable
{
    public void Render(RenderContext context)
    {
        if (!context.Options.ShowBoundingBoxEnabled)
            return;

        context.GraphGeometry.DrawLaneOutlines(context);
    }
}
