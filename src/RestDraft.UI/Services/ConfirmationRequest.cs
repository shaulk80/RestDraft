namespace RestDraft.UI.Services;

// A question that the user confirms or cancels: the window title, the question, and the text of the button that agrees.
public sealed record ConfirmationRequest(string Title, string Message, string ConfirmText)
{
    public static ConfirmationRequest Delete(string message)
    {
        return new ConfirmationRequest("Confirm delete", message, "Delete");
    }

    public static ConfirmationRequest Import(string message)
    {
        return new ConfirmationRequest("Confirm import", message, "Import");
    }
}
