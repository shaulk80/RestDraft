using Avalonia;
using Avalonia.Input;

namespace RestDraft.UI.Behaviors;

// Starts a drag of one item after the pointer has moved a few pixels from where it was pressed. A press that is
// released before that is a click, and Release hands the pressed item back so the caller can act on it.
public sealed class DragStarter<T>
    where T : class
{
    private const double DragThreshold = 4;

    private readonly DataFormat<T> _format;
    private PointerPressedEventArgs? _pressArgs;
    private T? _pressed;
    private Point _pressPoint;

    public DragStarter(DataFormat<T> format)
    {
        _format = format;
    }

    public void Press(T item, PointerPressedEventArgs args, Point position)
    {
        _pressed = item;
        _pressArgs = args;
        _pressPoint = position;
    }

    public void Reset()
    {
        _pressed = null;
        _pressArgs = null;
    }

    // The item of a press that has not become a drag, and forgets the press.
    public T? Release()
    {
        var clicked = _pressed;
        Reset();
        return clicked;
    }

    public async Task MoveAsync(Point position)
    {
        if (_pressed is not { } item || _pressArgs is not { } pressArgs)
        {
            return;
        }

        if (Math.Abs(position.X - _pressPoint.X) < DragThreshold && Math.Abs(position.Y - _pressPoint.Y) < DragThreshold)
        {
            return;
        }

        Reset();
        await DragDrop.DoDragDropAsync(pressArgs, DragTransfer.Create(_format, item), DragDropEffects.Move);
    }
}
