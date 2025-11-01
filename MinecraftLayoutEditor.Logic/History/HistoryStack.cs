using System.Xml.Linq;

namespace MinecraftLayoutEditor.Logic.History;

public class HistoryStack
{
    private readonly Stack<IHistoryAction> _undoStack = [];
    private readonly Stack<IHistoryAction> _redoStack = [];

    public bool CanUndo => _undoStack.Count != 0;
    public bool CanRedo => _redoStack.Count != 0;

    public void ExecuteAction(IHistoryAction action)
    {
        action.Execute();
        _undoStack.Push(action);
        _redoStack.Clear();
    }

    public void Undo()
    {
        if (_undoStack.Count == 0)
            return;

        var action = _undoStack.Pop();
        action.Undo();
        _redoStack.Push(action);
    }

    public void Redo()
    {
        if (_redoStack.Count == 0)
            return;

        var action = _redoStack.Pop();
        action.Execute();
        _undoStack.Push(action);
    }
}
