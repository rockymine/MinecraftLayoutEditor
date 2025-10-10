using Excubo.Blazor.Canvas;
using MinecraftLayoutEditor.WebApp.Extensions;
using MinecraftLayoutEditor.Logic;
using System.Numerics;
using MinecraftLayoutEditor.WebApp.Rendering;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace MinecraftLayoutEditor.WebApp.Pages;

public partial class Home
{
    private Canvas Canvas;
    private readonly Logic.Layout _layout = LayoutFactory.OneTeamDemo();
    private readonly LayoutRenderer _renderer = new();
    private readonly LayoutRenderOptions _options = new();
    private Node? HoveredNode;
    private Node? SelectedNode;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) 
            return;

        await Render();
    }

    private async Task OnMouseUp(MouseEventArgs e)
    {
        Vector2 clickedAt = new Vector2((float)e.OffsetX, (float)e.OffsetY);

        if (e.Button == 0 && HoveredNode == null)
        {
            _layout.Graph.Nodes.Add(new Node(clickedAt));
            await Render();
        }
        else if (e.Button == 0 && HoveredNode != null)
        {
            if (SelectedNode == null)
            {
                SelectedNode = HoveredNode;
            } 
            else
            {
                _layout.Graph.AddOrRemoveEdge(HoveredNode, SelectedNode);
                SelectedNode = null;
            }

            await Render();
        }
        else if (e.Button == 2 && HoveredNode != null)
        {
            _layout.Graph.DeleteNode(HoveredNode);
            HoveredNode = null;
            await Render();
        }            
    }

    private async Task OnMouseMove(MouseEventArgs e)
    {
        Vector2 cursorPosition = new Vector2((float)e.OffsetX, (float)e.OffsetY);
        Node? closestNode = _layout.Graph.GetClosestNode(cursorPosition);

        if (closestNode == null)
            return;

        var threshhold = 10;
        var distanceToClosestNode = Vector2.Distance(cursorPosition, closestNode.Position);

        var prevHovered = HoveredNode;
        HoveredNode = distanceToClosestNode <= threshhold ? closestNode : null;

        if (prevHovered != HoveredNode)
            await Render();
    }

    private async Task Render()
    {
        await using (var ctx = await Canvas.GetContext2DAsync())
        {
            await _renderer.RenderAsync(ctx, _layout, _options, HoveredNode, SelectedNode);
        }
    }
}