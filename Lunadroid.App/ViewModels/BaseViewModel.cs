using CommunityToolkit.Mvvm.ComponentModel;

namespace Lunadroid.App.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string _title = string.Empty;
}