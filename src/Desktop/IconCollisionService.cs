using Companion.Desktop.Icons;

namespace Companion.Desktop;

public sealed class IconCollisionService
{
    public bool IsVisualCollision((float X, float Y) petPosition, (float X, float Y, float W, float H) iconRect)
    {
        var withinX = petPosition.X >= iconRect.X && petPosition.X <= iconRect.X + iconRect.W;
        var withinY = petPosition.Y >= iconRect.Y && petPosition.Y <= iconRect.Y + iconRect.H;
        return withinX && withinY;
    }

    public bool IsVisualCollision((float X, float Y) petPosition, DesktopIconInfo icon)
    {
        return IsVisualCollision(petPosition, icon.ToRect());
    }

    public string BuildCollisionBubbleText(string iconName)
    {
        return $"碰到图标：{iconName}";
    }
}
