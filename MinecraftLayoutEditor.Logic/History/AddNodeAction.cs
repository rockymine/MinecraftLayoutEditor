using MinecraftLayoutEditor.Logic.Geometry;
using System.Numerics;

namespace MinecraftLayoutEditor.Logic.History;

public class AddNodeAction : IHistoryAction
{
    private readonly Graph _graph;
    private readonly Vector2 _position;
    private readonly Node.NodeType _nodeType;
    private readonly SymmetryAxis? _symmetry;
    private readonly bool _mirrorEnabled;

    private Node? _primaryNode;
    private Node? _mirroredNode;

    public AddNodeAction(Graph graph, Vector2 position, Node.NodeType nodeType, SymmetryAxis? symmetry, bool mirrorEnabled)
    {
        _graph = graph;
        _position = position;
        _nodeType = nodeType;
        _symmetry = symmetry;
        _mirrorEnabled = mirrorEnabled;
    }

    public void Execute()
    {
        var pos = new Vector2(float.Floor(_position.X) + 0.5f, float.Floor(_position.Y) + 0.5f);

        _primaryNode = new Node(pos) { Type = _nodeType };
        _graph.AddNode(_primaryNode);

        //TODO: mirrorEnabled redundant
        if (_symmetry != null && _mirrorEnabled)
        {
            var mirroredPos = Rotation.MirrorPosition(_primaryNode.Position, _symmetry);
            _mirroredNode = new Node(mirroredPos) 
            { 
                Type = _primaryNode.Type,
                MirrorRef = _primaryNode
            };

            _primaryNode.MirrorRef = _mirroredNode;
            _graph.AddNode(_mirroredNode);
        }
    }

    public void Undo()
    {
        //TODO: there will always be a node
        if (_primaryNode != null)
            _graph.DeleteNode(_primaryNode);
    }
}
