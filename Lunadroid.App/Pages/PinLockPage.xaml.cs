using Lunadroid.App.ViewModels;

namespace Lunadroid.App.Pages;

public partial class PinLockPage : ContentPage
{
    public PinLockPage(PinLockViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
