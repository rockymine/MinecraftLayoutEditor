using MinecraftLayoutEditor.Logic.Geometry;
using System.Numerics;

namespace MinecraftLayoutEditor.Logic;

public class Layout
{
    public Graph Graph { get; set; } = new Graph();
    public string Name { get; set; } = "layout";
    public int Width { get; set; }
    public int Height { get; set; }
    public List<Team> Teams { get; set; } = [];
    public string Author { get; set; } = "";

    public SymmetryAxis? Symmetry { get; set; }
    // TODO: Remove mirrorEnabled as it is the same as symmetry = null
    // move to ui only
    public bool MirrorEnabled { get; set; }
    public Node.NodeType SelectedNodeType { get; set; } = Node.NodeType.Undefined;

    public Vector2 MirrorPosition(Vector2 pos, SymmetryAxis axis)
    {
        if (axis.RotationDeg != 0)
        {
            return Rotation.RotateAboutOrigin(pos, Vector2.Zero, axis.RotationDeg * float.Pi / 180);
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

    public void MoveNode(Node node, Vector2 offset)
    {
        Vector2 newPosition = node.Position + offset;

        if (!Contains(newPosition))
            return;

        node.Position = newPosition;
        
        if (MirrorEnabled && node.MirrorRef != null && Symmetry != null)
        {
            var mirrorRefPosition = MirrorPosition(node.Position, Symmetry);
            node.MirrorRef.Position = mirrorRefPosition;
        }
    }
}
