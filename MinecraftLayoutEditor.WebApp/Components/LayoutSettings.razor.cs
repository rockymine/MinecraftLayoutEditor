using Microsoft.AspNetCore.Components;
using static MinecraftLayoutEditor.Logic.Layout;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class LayoutSettings
{
    [Parameter]
    public EventCallback SettingsChanged { get; set; }

    private int width;
    [Parameter]
    public int Width
    {
        get => width;
        set
        {
            if (width == value) return;

            width = value;
            WidthChanged.InvokeAsync(width);
            SettingsChanged.InvokeAsync();
        }
    }
    [Parameter]
    public EventCallback<int> WidthChanged { get; set; }

    private int height;
    [Parameter]
    public int Height
    {
        get => height;
        set
        {
            if (height == value) return;

            height = value;
            HeightChanged.InvokeAsync(height);
            SettingsChanged.InvokeAsync();
        }
    }
    [Parameter]
    public EventCallback<int> HeightChanged { get; set; }

    private bool mirrorEnabled;
    [Parameter]
    public bool MirrorEnabled
    {
        get => mirrorEnabled;
        set
        {
            if (mirrorEnabled == value) return;

            mirrorEnabled = value;
            MirrorEnabledChanged.InvokeAsync(mirrorEnabled);
            SettingsChanged.InvokeAsync();
        }
    }

    [Parameter]
    public EventCallback<bool> MirrorEnabledChanged { get; set; }

    private float rotationDeg;
    [Parameter]
    public float RotationDeg
    {
        get => rotationDeg;
        set
        {
            if (rotationDeg == value) return;

            rotationDeg = value;
            RotationDegChanged.InvokeAsync(rotationDeg);
            SettingsChanged.InvokeAsync();
        }
    }

    [Parameter]
    public EventCallback<float> RotationDegChanged { get; set; }

    private bool isHorizontal = false; // false = Vertical (default)
    [Parameter]
    public bool IsHorizontal
    {
        get => isHorizontal;
        set
        {
            if (isHorizontal == value) return;

            isHorizontal = value;
            IsHorizontalChanged.InvokeAsync(value);
            SettingsChanged.InvokeAsync();
        }
    }
    [Parameter] public EventCallback<bool> IsHorizontalChanged { get; set; }

    private string AxisText
    {
        get => IsHorizontal ? "Horizontal" : "Vertical";
        set
        {
            var newIsHorizontal = value == "Horizontal";
            if (newIsHorizontal == IsHorizontal) return;

            IsHorizontal = newIsHorizontal;
        }
    }

    private bool showBlocksEnabled;
    [Parameter]
    public bool ShowBlocksEnabled
    {
        get => showBlocksEnabled;
        set
        {
            if (showBlocksEnabled == value) return;
            
            showBlocksEnabled = value;
            ShowBlocksEnabledChanged.InvokeAsync(showBlocksEnabled);
            SettingsChanged.InvokeAsync();
        }
    }
    [Parameter]
    public EventCallback<bool> ShowBlocksEnabledChanged { get; set; }
}