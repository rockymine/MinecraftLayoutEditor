using SkiaSharp;
using System.Numerics;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class Viewport
{
    public float CanvasWidth { get; private set; }
    public float CanvasHeight { get; private set; }
    public Vector2 CameraPosition { get; private set; }
    public SKMatrix SKWorldToScreen;
    public float Scale { get; set; } = 1f;
    public Vector2 Center => new(CanvasWidth / 2f, CanvasHeight / 2f);

    public void Resize(float width, float height)
    {
        CanvasWidth = width;
        CanvasHeight = height;
    }

    public void UpdateTRS(Vector2 translation)
    {
        CameraPosition = translation;

        SKWorldToScreen = SKMatrix.CreateScale(Scale, Scale);
        SKWorldToScreen.TransX = translation.X;
        SKWorldToScreen.TransY = translation.Y;
    }

    public Vector2 ScreenToWorldPos(Vector2 screen)
    {
        var worldPosX = (screen.X - CameraPosition.X) / Scale;
        var worldPosY = (screen.Y - CameraPosition.Y) / Scale;
        return new(worldPosX, worldPosY);
    }

    public float CalculateMinZoom(float width, float height)
    {
        if (width <= 0 || height <= 0) 
            return 1f;

        float scaleX = CanvasWidth / width;
        float scaleY = CanvasHeight / height;
        return float.Min(scaleX, scaleY) * 0.98f;
    }

    public float CalculateMaxZoom()
    {
        float scaleX = CanvasWidth / 16f;
        float scaleY = CanvasHeight / 16f;
        return float.Min(scaleX, scaleY) * 0.98f;
    }
}
