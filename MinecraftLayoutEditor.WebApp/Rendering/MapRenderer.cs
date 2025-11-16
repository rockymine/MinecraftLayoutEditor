using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class MapRenderer
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
        RenderContext.Surface!.Canvas.SetMatrix(RenderContext.Viewport.SKWorldToScreen);
        
        foreach (var renderable in renderables)
        {
            renderable.Render(RenderContext);
        }
    }
}
