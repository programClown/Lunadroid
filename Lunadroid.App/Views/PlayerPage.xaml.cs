using Lunadroid.App.Models;
using Lunadroid.App.ViewModels;
using UraniumUI.Extensions;

namespace Lunadroid.App.Views;

public partial class PlayerPage : ContentPage, IQueryAttributable
{
    public PlayerPage(PlayerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Online", out object? movieObj) && movieObj is VedioSearchResult movie)
        {
            var vm = (PlayerViewModel)BindingContext;
            vm.SetOnLineVideoAsync(movie).FireAndForget();
        }
        else if (query.TryGetValue("Local", out object? localObj) && localObj is string playUrl)
        {
            var vm = (PlayerViewModel)BindingContext;
            vm.SetLocalVideoAsync(playUrl).FireAndForget();
        }
    }
}