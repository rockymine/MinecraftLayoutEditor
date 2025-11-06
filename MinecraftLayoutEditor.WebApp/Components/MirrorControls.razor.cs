using Microsoft.AspNetCore.Components;
using MinecraftLayoutEditor.Logic;

namespace MinecraftLayoutEditor.WebApp.Components;

public partial class MirrorControls
{
    [Parameter]
    public EventCallback SettingsChanged { get; set; }

    [Parameter]
    public SymmetryAxis? SymmetryAxis { get; set; }
    [Parameter]
    public EventCallback<SymmetryAxis?> SymmetryAxisChanged { get; set; }

    [Parameter]
    public float RotationDeg { get; set; }
    [Parameter]
    public EventCallback<float> RotationDegChanged { get; set; }

    [Parameter]
    public bool IsHorizontal { get; set; }
    [Parameter] public EventCallback<bool> IsHorizontalChanged { get; set; }

    private bool IsMirrorEnabled => SymmetryAxis != null;

    public async Task OnSymmetryAxisChanged(bool enabled)
    {
        if (enabled)
        {
            SymmetryAxis = new SymmetryAxis()
            {
                RotationDeg = RotationDeg,
                IsHorizontal = IsHorizontal,
            };
        }
        else
        {
            SymmetryAxis = null;
        }

        await SymmetryAxisChanged.InvokeAsync(SymmetryAxis);
        await SettingsChanged.InvokeAsync();
    }

    public async Task OnRotationDegChanged()
    {
        await RotationDegChanged.InvokeAsync(RotationDeg);

        if (IsMirrorEnabled)
        {
            SymmetryAxis = new SymmetryAxis()
            {
                RotationDeg = RotationDeg,
                IsHorizontal = IsHorizontal,
            };

            await SymmetryAxisChanged.InvokeAsync(SymmetryAxis);
        }

        await SettingsChanged.InvokeAsync();
    }

    public async Task OnIsHorizontalChanged()
    {
        await IsHorizontalChanged.InvokeAsync(IsHorizontal);

        if (IsMirrorEnabled)
        {
            SymmetryAxis = new SymmetryAxis()
            {
                RotationDeg = RotationDeg,
                IsHorizontal = IsHorizontal,
            };

            await SymmetryAxisChanged.InvokeAsync(SymmetryAxis);
        }

        await SettingsChanged.InvokeAsync();
    }
}