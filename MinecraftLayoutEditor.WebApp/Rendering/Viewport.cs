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

    public void FitToContent(float width, float height)
    {
        Scale = CalculateMinZoom(width, height);
        UpdateTRS(Center);
    }

    public void FitToSection(float width, float height, bool horizontalSplit)
    {
        float halfW = width / 2f;
        float halfH = height / 2f;

        Vector2 mapHalfCenter;
        float scaleX, scaleY;

        // Horizontal mirror
        if (horizontalSplit)
        {
            mapHalfCenter = new Vector2(0, -halfH / 2f);
            scaleX = CanvasWidth / width;
            scaleY = CanvasHeight / halfH;
        }
        // Vertical mirror
        else
        {
            mapHalfCenter = new Vector2(halfW / 2f, 0);
            scaleX = CanvasWidth / halfW;
            scaleY = CanvasHeight / height;
        }

        Scale = float.Min(scaleX, scaleY) * 0.98f;
        var newTranslation = Center - mapHalfCenter * Scale;
        UpdateTRS(newTranslation);
    }

    public bool TryZoom(double deltaY, Vector2 cursorPos, float width, float height, float minSize = 16f)
    {
        if (deltaY == 0)
            return false;

        var worldPosBeforeZoom = ScreenToWorldPos(cursorPos);
        float newScale = deltaY < 0 ? Scale * 1.6f : Scale / 1.6f;
        var minZoom = CalculateMinZoom(width, height);
        var maxZoom = CalculateMaxZoom();
        newScale = float.Clamp(newScale, minZoom, maxZoom);

        if (Math.Abs(newScale - Scale) < 0.001f)
            return false;

        // Keep cursor position
        var newTranslation = cursorPos - worldPosBeforeZoom * newScale;

        Scale = newScale;
        UpdateTRS(newTranslation);
        return true;
    }
}
