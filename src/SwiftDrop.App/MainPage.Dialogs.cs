namespace SwiftDrop.App;

public partial class MainPage
{
    private new Task DisplayAlert(string title, string message, string cancel)
        => DisplayAlertAsync(title, message, cancel);

    private new Task<bool> DisplayAlert(string title, string message, string accept, string cancel)
        => DisplayAlertAsync(title, message, accept, cancel);

    private new Task<string?> DisplayActionSheet(
        string title,
        string cancel,
        string? destruction,
        params string[] buttons)
        => DisplayActionSheetAsync(title, cancel, destruction, buttons);
}
