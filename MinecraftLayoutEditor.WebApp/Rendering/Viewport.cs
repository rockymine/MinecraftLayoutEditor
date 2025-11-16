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
        => new((screen.X - CameraPosition.X) / Scale, (screen.Y - CameraPosition.Y) / Scale);
}
