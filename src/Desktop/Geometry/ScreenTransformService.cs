namespace Companion.Desktop.Geometry;

public sealed class ScreenTransformService
{
    public (float X, float Y) ToLogicalPoint((float X, float Y) physicalPoint, float dpiScale)
    {
        if (dpiScale <= 0)
        {
            dpiScale = 1;
        }

        return (physicalPoint.X / dpiScale, physicalPoint.Y / dpiScale);
    }

    public (float X, float Y, float W, float H) ToLogicalRect((float X, float Y, float W, float H) physicalRect, float dpiScale)
    {
        if (dpiScale <= 0)
        {
            dpiScale = 1;
        }

        return (
            physicalRect.X / dpiScale,
            physicalRect.Y / dpiScale,
            physicalRect.W / dpiScale,
            physicalRect.H / dpiScale);
    }
}
