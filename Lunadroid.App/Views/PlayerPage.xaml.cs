using Lunadroid.App.Models;
using Lunadroid.App.ViewModels;
using UraniumUI.Extensions;
using UraniumUI.Pages;

namespace Lunadroid.App.Views;

public partial class PlayerPage : UraniumContentPage, IQueryAttributable
{
    public PlayerPage(PlayerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Online", out var movieObj) && movieObj is VedioSearchResult movie)
        {
            var vm = (PlayerViewModel)BindingContext;
            vm.SetOnLineVideoAsync(movie).FireAndForget();
        }
        else if (query.TryGetValue("Local", out var localObj) && localObj is string playUrl)
        {
            var vm = (PlayerViewModel)BindingContext;
            vm.SetLocalVideoAsync(playUrl).FireAndForget();
        }
    }

    protected override bool OnBackButtonPressed()
    {
        if (VideoPlayer.IsFullscreen)
        {
            VideoPlayer.ExitFullscreen();
            return true;
        }

        return base.OnBackButtonPressed();
    }
}