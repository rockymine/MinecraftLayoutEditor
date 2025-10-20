using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftLayoutEditor.Logic.History;

public interface IHistoryAction
{
    void Execute();
    void Undo();
}
