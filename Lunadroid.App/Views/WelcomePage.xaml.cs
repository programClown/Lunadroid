using Lunadroid.App.ViewModels;
using UraniumUI.Pages;

namespace Lunadroid.App.Views;

public partial class WelcomePage : UraniumContentPage
{
    public WelcomePage()
    {
        InitializeComponent();
        BindingContext = App.Services.GetRequiredService<WelcomeViewModel>();
    }
}