using Avalonia.Input;

namespace RestDraft.UI.Behaviors;

public static class DragTransfer
{
    // What a drag carries: the item itself, in a format that only this process understands.
    public static DataTransfer Create<T>(DataFormat<T> format, T item)
        where T : class
    {
        var data = new DataTransferItem();
        data.Set(format, item);
        var transfer = new DataTransfer();
        transfer.Add(data);
        return transfer;
    }
}
