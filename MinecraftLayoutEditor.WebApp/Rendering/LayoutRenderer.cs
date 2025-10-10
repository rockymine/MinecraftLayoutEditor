using Excubo.Blazor.Canvas.Contexts;
using MinecraftLayoutEditor.Logic;
using MinecraftLayoutEditor.WebApp.Extensions;
using Excubo.Blazor.Canvas;
using System.Xml.Linq;

namespace MinecraftLayoutEditor.WebApp.Rendering;

public class LayoutRenderer
{
    public async Task RenderAsync(Context2D ctx, Logic.Layout layout, LayoutRenderOptions opt,
        Node? hoveredNode, Node? selectedNode)
    {
        var edges = new List<Edge>();
        var nodes = layout.Graph.Nodes;

        foreach (var n in nodes)
        {
            foreach (var e in n.Edges)
            {
                if (!edges.Contains(e))
                    edges.Add(e); 
            }
        }

        await ctx.ClearRectAsync(0, 0, 1000, 1000);
        await RenderEdges(ctx, edges, opt);
        await RenderNodes(ctx, nodes, opt, hoveredNode, selectedNode);
    }

    private async Task RenderNodes(Context2D ctx, List<Node> nodes, LayoutRenderOptions opt, 
        Node? hoveredNode, Node? selectedNode)
    {
        foreach (var n in nodes)
        {
            var fill = GetNodeFillColor(n, selectedNode, hoveredNode, opt);

            await ctx.DrawCircle(n.Position, opt.DefaultRadius, opt.DefaultRadius, 
                opt.EdgeWidth, fill, opt.DefaultStroke, FillRule.NonZero);
        }
    }

    private async Task RenderEdges(Context2D ctx, List<Edge> edges, LayoutRenderOptions opt)
    {
        foreach (var e in edges)
        {
            var dash = opt.DefaultDash;
            if (e.Type == Edge.EdgeType.Bridgeable)
                dash = [5];
            
            await ctx.DrawLine(e.Node1.Position, e.Node2.Position, opt.EdgeWidth, 
                opt.DefaultStroke, dash);
        }
    }

    private string GetNodeFillColor(Node node, Node? selectedNode, Node? hoveredNode, LayoutRenderOptions opt)
    {
        if (selectedNode == null && hoveredNode == null)
            return opt.DefaultFill;

        if (node == selectedNode)
            return opt.SelectedNodeFill;

        if (node == hoveredNode)
            return opt.HoveredNodeFill;

        return opt.DefaultFill;
    }
}

public class LayoutRenderOptions
{
    public double EdgeWidth { get; init; } = 2;
    public string EdgeColor { get; init; } = "#333";
    public double SpawnRadius { get; init; } = 10;
    public double WoolRadius { get; init; } = 9;
    public double DefaultRadius { get; init; } = 6;
    public string SpawnFill { get; init; } = "limegreen";
    public string SpawnStroke { get; init; } = "#0a3";
    public string WoolFill { get; init; } = "gold";
    public string WoolStroke { get; init; } = "#b98c00";
    public string DefaultFill { get; init; } = "lightgray";
    public string DefaultStroke { get; init; } = "#666";
    public bool ShowLabels { get; init; } = false;
    public string LabelFont { get; init; } = "12px system-ui, sans-serif";
    public string LabelColor { get; init; } = "#111";
    public double[] DefaultDash { get; init; } = [];
    public string HoveredNodeFill { get; init; } = "purple";
    public string SelectedNodeFill { get; init; } = "yellow";
}
