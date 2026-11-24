using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace RestDraft.UI.Behaviors;

// Draws where a drop will land: a line along one edge of a control, or a frame around it. The border is an overlay
// in the same panel as the controls it points at, and host is the panel its margin is measured in.
public sealed class DropIndicator
{
    private const double LineThickness = 2;
    private const string LineClass = "dropLine";
    private const string FrameClass = "dropFrame";

    private readonly Border _border;
    private readonly Visual _host;

    public DropIndicator(Border border, Visual host)
    {
        _border = border;
        _host = host;
        _border.HorizontalAlignment = HorizontalAlignment.Left;
        _border.VerticalAlignment = VerticalAlignment.Top;
        _border.IsHitTestVisible = false;
        _border.IsVisible = false;
    }

    public void ShowHorizontalLine(Control target, bool atBottom)
    {
        var bounds = BoundsOf(target);
        var y = atBottom ? bounds.Bottom : bounds.Top;
        Show(LineClass, new Rect(bounds.X, y - (LineThickness / 2), bounds.Width, LineThickness));
    }

    public void ShowVerticalLine(Control target, bool atRight)
    {
        var bounds = BoundsOf(target);
        var x = atRight ? bounds.Right : bounds.Left;
        Show(LineClass, new Rect(x - (LineThickness / 2), bounds.Y, LineThickness, bounds.Height));
    }

    public void ShowFrame(Control target)
    {
        Show(FrameClass, BoundsOf(target));
    }

    public void Hide()
    {
        _border.IsVisible = false;
    }

    private Rect BoundsOf(Control target)
    {
        var origin = target.TranslatePoint(new Point(0, 0), _host) ?? new Point();
        return new Rect(origin, target.Bounds.Size);
    }

    private void Show(string kind, Rect area)
    {
        _border.Classes.Set(LineClass, kind == LineClass);
        _border.Classes.Set(FrameClass, kind == FrameClass);
        _border.Margin = new Thickness(area.X, area.Y, 0, 0);
        _border.Width = area.Width;
        _border.Height = area.Height;
        _border.IsVisible = true;
    }
}
