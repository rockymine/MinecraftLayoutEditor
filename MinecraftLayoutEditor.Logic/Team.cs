using System;
using System.Collections.Generic;
using System.Text;

namespace MinecraftLayoutEditor.Logic;

public class Team
{
    public MinecraftColor Color { get; set; }
    public List<Goal> Goals { get; set; } = [];
}

public enum MinecraftColor
{
    WHITE,
    ORANGE,
    MAGENTA,
    LIGHT_BLUE,
    YELLOW,
    LIME,
    PINK,
    GRAY,
    SILVER,
    CYAN,
    PURPLE,
    BLUE,
    BROWN,
    GREEN,
    RED,
    BLACK
}


