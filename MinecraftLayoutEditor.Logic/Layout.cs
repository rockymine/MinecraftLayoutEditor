using System;
using System.Collections.Generic;
using System.Text;

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

    public enum MirrorMode
    {
        None,
        Rotate180,
        Mirror,
        Mirror2
    }
}
