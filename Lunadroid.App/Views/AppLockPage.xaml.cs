using Lunadroid.App.ViewModels;
using UraniumUI.Pages;

namespace Lunadroid.App.Views;

public partial class AppLockPage : UraniumContentPage
{
    public AppLockPage(bool isVerifying = false)
    {
        InitializeComponent();
        var vm = App.Services.GetRequiredService<AppLockViewModel>();
        BindingContext = vm;
    }
}