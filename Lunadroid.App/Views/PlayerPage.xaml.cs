using Lunadroid.App.ViewModels;

namespace Lunadroid.App.Views;

public partial class PlayerPage
{
    public PlayerPage(PlayerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        var vm = (PlayerViewModel)BindingContext;

        string? playUrl = Shell.Current.CurrentItem?.CurrentItem?.ToString();
        var queryParams = ParseQueryParameters(playUrl);

        if (queryParams.TryGetValue("isLocal", out string? isLocal) && isLocal == "true")
        {
            if (queryParams.TryGetValue("playUrl", out string? url))
            {
                string title = queryParams.TryGetValue("title", out string? t) ? Uri.UnescapeDataString(t) : "本地视频";
                vm.LoadFromUrl(Uri.UnescapeDataString(url), title);
            }
        }
        else
        {
            if (queryParams.TryGetValue("source", out string? source) &&
                queryParams.TryGetValue("vodId", out string? vodId))
            {
                bool isAdult = queryParams.TryGetValue("isAdult", out string? adult) && adult == "true";
                await vm.LoadDetailAsync(source, vodId, isAdult);
            }
            else if (queryParams.TryGetValue("playUrl", out string? url))
            {
                string title = queryParams.TryGetValue("title", out string? t) ? Uri.UnescapeDataString(t) : string.Empty;
                vm.LoadFromUrl(Uri.UnescapeDataString(url), title);
            }
        }
    }

    private static Dictionary<string, string> ParseQueryParameters(string? url)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(url)) return result;

        int queryStart = url.IndexOf('?');
        if (queryStart < 0) return result;

        string query = url[(queryStart + 1)..];
        foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] kv = pair.Split('=', 2);
            if (kv.Length == 2)
            {
                result[kv[0]] = kv[1];
            }
        }

        return result;
    }
}