namespace Companion.Desktop.Icons;

public sealed record DesktopIconInfo(
    string Name,
    float X,
    float Y,
    float Width,
    float Height)
{
    public (float X, float Y, float W, float H) ToRect() => (X, Y, Width, Height);
}
