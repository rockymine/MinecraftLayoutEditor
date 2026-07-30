using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace MinecraftLayoutEditor.WebApp.Rendering;

/// <summary>
/// Frame and per-renderer timings for the canvas. A slow frame is only actionable
/// once it can be attributed to the renderable that spent the time, so every
/// renderable is timed separately and the totals are kept over a rolling window.
/// </summary>
public class RenderProfiler
{
    private const int SampleCapacity = 240;

    private readonly Dictionary<string, RendererTotals> _perRenderer = [];
    private readonly Queue<double> _frameMilliseconds = new();
    private readonly Stopwatch _frameTimer = new();
    private readonly Stopwatch _rendererTimer = new();

    private string _currentRenderer = string.Empty;

    public int FrameCount { get; private set; }
    public double LastFrameMilliseconds { get; private set; }

    /// <summary>
    /// How many times the Blazor component re-rendered and re-diffed its markup. This
    /// is separate from painting the canvas: a component render walks the whole sidebar
    /// and costs nothing visible when none of it changed.
    /// </summary>
    public int ComponentRenders { get; private set; }

    public void RecordComponentRender() => ComponentRenders++;

    /// <summary>
    /// Time spent resolving which node the pointer is over, totalled over the window.
    /// This happens per pointer move rather than per frame, so it is tracked separately
    /// from the renderables.
    /// </summary>
    public double HoverTotalMilliseconds { get; private set; }
    public int HoverLookups { get; private set; }

    public double AverageHoverMilliseconds =>
        HoverLookups == 0 ? 0 : HoverTotalMilliseconds / HoverLookups;

    public void RecordHoverLookup(double milliseconds)
    {
        HoverTotalMilliseconds += milliseconds;
        HoverLookups++;
    }

    public double AverageFrameMilliseconds
    {
        get
        {
            if (_frameMilliseconds.Count == 0)
                return 0;

            double total = 0;
            foreach (var sample in _frameMilliseconds)
                total += sample;

            return total / _frameMilliseconds.Count;
        }
    }

    public double PeakFrameMilliseconds
    {
        get
        {
            double peak = 0;
            foreach (var sample in _frameMilliseconds)
                peak = Math.Max(peak, sample);

            return peak;
        }
    }

    public void BeginFrame()
    {
        _frameTimer.Restart();
    }

    public void EndFrame()
    {
        _frameTimer.Stop();
        LastFrameMilliseconds = _frameTimer.Elapsed.TotalMilliseconds;
        FrameCount++;

        _frameMilliseconds.Enqueue(LastFrameMilliseconds);
        while (_frameMilliseconds.Count > SampleCapacity)
            _frameMilliseconds.Dequeue();
    }

    public void BeginRenderer(string rendererName)
    {
        _currentRenderer = rendererName;
        _rendererTimer.Restart();
    }

    public void EndRenderer()
    {
        _rendererTimer.Stop();

        if (!_perRenderer.TryGetValue(_currentRenderer, out var totals))
        {
            totals = new RendererTotals();
            _perRenderer[_currentRenderer] = totals;
        }

        totals.Calls++;
        totals.TotalMilliseconds += _rendererTimer.Elapsed.TotalMilliseconds;
    }

    public void Reset()
    {
        _perRenderer.Clear();
        _frameMilliseconds.Clear();
        FrameCount = 0;
        ComponentRenders = 0;
        HoverTotalMilliseconds = 0;
        HoverLookups = 0;
        LastFrameMilliseconds = 0;
    }

    /// <summary>
    /// The rolling window as JSON: frame counts, average and peak frame cost, and the
    /// average cost of each renderable per frame, worst first.
    /// </summary>
    public string ToJson()
    {
        var json = new StringBuilder();
        json.Append("{\"frames\":").Append(FrameCount);
        json.Append(",\"avgFrameMs\":").Append(Format(AverageFrameMilliseconds));
        json.Append(",\"peakFrameMs\":").Append(Format(PeakFrameMilliseconds));
        json.Append(",\"lastFrameMs\":").Append(Format(LastFrameMilliseconds));
        json.Append(",\"componentRenders\":").Append(ComponentRenders);
        json.Append(",\"hoverLookups\":").Append(HoverLookups);
        json.Append(",\"avgHoverMs\":").Append(Format(AverageHoverMilliseconds));
        json.Append(",\"renderers\":{");

        var ordered = _perRenderer
            .OrderByDescending(entry => entry.Value.TotalMilliseconds)
            .ToList();

        for (int index = 0; index < ordered.Count; index++)
        {
            var (rendererName, totals) = (ordered[index].Key, ordered[index].Value);
            var averageMilliseconds = totals.Calls == 0
                ? 0
                : totals.TotalMilliseconds / totals.Calls;

            if (index > 0)
                json.Append(',');

            json.Append('"').Append(rendererName).Append("\":")
                .Append(Format(averageMilliseconds));
        }

        json.Append("}}");
        return json.ToString();
    }

    public string ToText()
    {
        var average = AverageFrameMilliseconds;
        var fps = average > 0 ? 1000 / average : 0;

        var text = new StringBuilder();
        text.Append(Format(average)).Append(" ms/frame (")
            .Append(fps.ToString("F1", CultureInfo.InvariantCulture)).Append(" fps), peak ")
            .Append(Format(PeakFrameMilliseconds)).Append(" ms, ")
            .Append(FrameCount).Append(" frames, ")
            .Append(ComponentRenders).Append(" component renders");

        foreach (var entry in _perRenderer.OrderByDescending(pair => pair.Value.TotalMilliseconds))
        {
            var averageForRenderer = entry.Value.Calls == 0
                ? 0
                : entry.Value.TotalMilliseconds / entry.Value.Calls;

            if (averageForRenderer < 0.05)
                continue;

            text.Append('\n').Append(entry.Key.Replace("Renderer", string.Empty))
                .Append(": ").Append(Format(averageForRenderer)).Append(" ms");
        }

        return text.ToString();
    }

    private static string Format(double value) =>
        value.ToString("F2", CultureInfo.InvariantCulture);

    private sealed class RendererTotals
    {
        public int Calls { get; set; }
        public double TotalMilliseconds { get; set; }
    }
}
