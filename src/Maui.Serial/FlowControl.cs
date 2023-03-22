namespace Maui.Serial;

[Flags]
public enum FlowControl
{
    None = 0,
    RtsCtsIn = 1,
    RtsCtsOut = 2,
    XOnXOffIn = 4,
    XOnXOffOut = 8
}

