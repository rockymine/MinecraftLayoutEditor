using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.Logic.Geometry;
using MinecraftLayoutEditor.XML;
using SkiaSharp;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class MapRenderer
{  
    public float CanvasWidth { get; private set; }
    public float CanvasHeight { get; private set; }
    public Vector2 CameraPosition { get; private set; }
    public RenderContext RenderContext { get; set; }
    public List<IRenderable> renderables = [];

    private SKMatrix SKWorldToScreen;

    public MapRenderer(RenderContext renderContext)
    {
        RenderContext = renderContext;
        UpdateTRS(new Vector2(25, 25));
    }

    public void Resize(float width, float height)
    {
        CanvasWidth = width;
        CanvasHeight = height;
    }

    public void UpdateTRS(Vector2 translation)
    {
        CameraPosition = translation;

        SKWorldToScreen = SKMatrix.CreateScale(RenderContext.Scale, RenderContext.Scale);
        SKWorldToScreen.TransX = translation.X;
        SKWorldToScreen.TransY = translation.Y;
    }

    public void Render()
    {
        RenderContext.Surface!.Canvas.SetMatrix(SKWorldToScreen);
        
        foreach (var renderable in renderables)
        {
            renderable.Render(RenderContext);
        }
    }

    public Vector2 ScreenToWorldPos(Vector2 screen)
        => new((screen.X - CameraPosition.X) / RenderContext.Scale, (screen.Y - CameraPosition.Y) / RenderContext.Scale);
}
