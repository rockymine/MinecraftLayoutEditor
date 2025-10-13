using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace MinecraftLayoutEditor.Logic;

public class Layout
{
    public Graph Graph { get; set; } = new Graph();
    public string Name { get; set; } = "New Layout";
    public int Width { get; set; }
    public int Height { get; set; }
    public List<Team> Teams { get; set; } = [];
    public string Author { get; set; } = "";
    public MirrorMode Geometry { get; set; }
    public SymmetryAxis? Symmetry { get; set; }
    public bool MirrorEnabled { get; set; }
    public bool ShowBlocksEnabled { get; set; }

    public enum MirrorMode
    {
        None,
        Rotate180,
        MirrorHorizontal,
        MirrorVertical
    }

    public Vector2 MirrorPosition(Vector2 pos, SymmetryAxis axis)
    {
        if (axis.RotationDeg != 0)
        {
            return RotateAboutOrigin(pos, Vector2.Zero, axis.RotationDeg * float.Pi / 180);
        }

        if (axis.IsHorizontal)
        {
            var isAboveAxis = pos.Y < axis.Offset;
            var distToAxis = Math.Abs(pos.Y - axis.Offset);

            return isAboveAxis ? new Vector2(pos.X, axis.Offset + distToAxis)
                : new Vector2(pos.X, axis.Offset - distToAxis);
        } 
        else
        {
            var isLeftOfAxis = pos.X < axis.Offset;
            var distToAxis = Math.Abs(pos.X - axis.Offset);

            return isLeftOfAxis ? new Vector2(axis.Offset + distToAxis, pos.Y)
                : new Vector2(axis.Offset - distToAxis, pos.Y);
        }
    }

    public bool Contains(Vector2 pos)
    {
        return pos.X >= -Width / 2 && pos.X <= Width / 2
            && pos.Y >= -Height / 2 && pos.Y <= Height / 2;
    }

    public void AddNode(Vector2 pos)
    {
        pos = new Vector2(float.Floor(pos.X) + 0.5f, float.Floor(pos.Y) + 0.5f);

        var closestNode = Graph.GetClosestNode(pos);

        // node already exists
        if (closestNode != null && Vector2.DistanceSquared(closestNode.Position, pos) < 1)
            return;

        var node = new Node(pos);
        Graph.Nodes.Add(node);

        if (Symmetry == null)
            return;

        if (MirrorEnabled)
        {
            var mirroredPos = MirrorPosition(node.Position, Symmetry);

            var mirrored = new Node(mirroredPos)
            {
                MirrorRef = node
            };

            node.MirrorRef = mirrored;
            Graph.Nodes.Add(mirrored);
        }        
    }

    // TODO: move to utility class
    public Vector2 RotateAboutOrigin(Vector2 point, Vector2 origin, float rotation)
    {
        return Vector2.Transform(point - origin, Matrix3x2.CreateRotation(rotation)) + origin;
    }
}
