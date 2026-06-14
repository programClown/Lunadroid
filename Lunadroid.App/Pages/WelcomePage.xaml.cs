namespace Lunadroid.App.Pages;

public partial class WelcomePage : ContentPage
{
    public WelcomePage()
    {
        InitializeComponent();
    }

    private void OnStartClicked(object? sender, EventArgs e)
    {
        Application.Current!.Windows[0].Page = new TermsPage();
    }
}
