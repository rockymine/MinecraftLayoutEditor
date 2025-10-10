using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace MinecraftLayoutEditor.Logic;

public class Graph
{
    public List<Node> Nodes { get; set; } = [];

    public Node? GetClosestNode(Vector2 pos)
    {
        if (Nodes.Count == 0)
            return null;

        Node? closestNode = null;
        var closestDistance = double.MaxValue;

        foreach (var n in Nodes)
        {
            var distance = Vector2.Distance(pos, n.Position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNode = n;
            } 
        }

        return closestNode;
    }

    public void DeleteNode(Node node)
    {
        if (node.MirrorRef != null)
        {
            var mirrorRef = node.MirrorRef;
            node.MirrorRef.MirrorRef = null;
            node.MirrorRef = null;
            DeleteNode(mirrorRef);
        }            
        
        foreach (var e in node.Edges)
        {
            if (e.Node1 == node)
            {
                e.Node2.Edges.Remove(e);
            }
            else if (e.Node2 == node)
            {
                e.Node1.Edges.Remove(e);
            }
        }

        Nodes.Remove(node); 
    }

    public void DeleteEdge(Edge edge)
    {
        edge.Node1.Edges.Remove(edge);
        edge.Node2.Edges.Remove(edge);
    }

    public void AddOrRemoveEdge(Node node1, Node node2, bool isMirror = false)
    {
        if (node1 == node2)
            throw new InvalidOperationException();

        if (node1.MirrorRef != null && node2.MirrorRef != null && !isMirror
            && node1.MirrorRef != node2)
        {
            AddOrRemoveEdge(node1.MirrorRef, node2.MirrorRef, true);
        }

        var edge1 = node1.EdgeTo(node2);
        var edge2 = node2.EdgeTo(node1);

        var anyRemoved = false;
        
        if (edge1 != null && edge2 != null)
        {
            DeleteEdge(edge1);

            if (edge1 != edge2) 
                DeleteEdge(edge2);

            anyRemoved = true;
        } 
        else if (edge1 != null)
        {
            DeleteEdge(edge1);
            anyRemoved = true;
        } 
        else if (edge2 != null)
        {
            DeleteEdge(edge2);
            anyRemoved = true;
        }

        if (anyRemoved)
            return;

        var edge = new Edge(node1, node2);
        node1.Edges.Add(edge);
        node2.Edges.Add(edge);
    }
}
