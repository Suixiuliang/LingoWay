using System.Text.RegularExpressions;

namespace LingoWay.Views;

public partial class DictPopupPage : ContentPage
{
    private readonly string? _detailText;

    public DictPopupPage(string word, string? detailText)
    {
        InitializeComponent();
        _detailText = detailText;
        TitleLabel.Text = word;
        PopulateTabs(detailText);
    }

    /// <summary>Show as modal from any page</summary>
    public static async Task ShowAsync(Page host, string word, string? detailText)
    {
        var popup = new DictPopupPage(word, detailText);
        await host.Navigation.PushModalAsync(popup, animated: false);
    }

    // ═══════ Tab switching ═══════

    private void SetActiveTab(string source)
    {
        ContentOxford.IsVisible = source == "oxford";
        ContentCollins.IsVisible = source == "collins";
        ContentUrban.IsVisible   = source == "urban";

        void Style(Border tab, Label label, bool active)
        {
            tab.BackgroundColor    = Color.FromArgb(active ? "#3B3B40"  : "#27272A");
            tab.Stroke             = Color.FromArgb(active ? "#005FB8"  : "#00000000");
            tab.StrokeThickness    = active ? 2 : 0;
            label.TextColor        = Color.FromArgb(active ? "#FFFFFF"  : "#9CA3AF");
            label.FontAttributes   = active ? FontAttributes.Bold : FontAttributes.None;
        }

        Style(TabOxford,  LabelOxford,  source == "oxford");
        Style(TabCollins, LabelCollins, source == "collins");
        Style(TabUrban,   LabelUrban,   source == "urban");
    }

    private void OnOxfordTabTapped (object? s, EventArgs e) => SetActiveTab("oxford");
    private void OnCollinsTabTapped(object? s, EventArgs e) => SetActiveTab("collins");
    private void OnUrbanTabTapped  (object? s, EventArgs e) => SetActiveTab("urban");

    private async void OnCloseTapped(object? s, EventArgs e) => await Navigation.PopModalAsync(animated: false);
    private async void OnBackdropTapped(object? s, EventArgs e) => await Navigation.PopModalAsync(animated: false);

    // ═══════ Content population ═══════

    private void PopulateTabs(string? detailText)
    {
        if (string.IsNullOrWhiteSpace(detailText)) return;

        var sections = Regex.Split(detailText, @"\n?────────\n?");
        foreach (var section in sections)
        {
            var trimmed = section.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            var headerMatch = Regex.Match(trimmed, @"^【([^】]+)】(.*)", RegexOptions.Singleline);
            if (!headerMatch.Success) continue;

            var header = headerMatch.Groups[1].Value.Trim();
            var body   = headerMatch.Groups[2].Value.Trim();

            string key = header.Contains("牛津") ? "oxford" :
                         header.Contains("柯林斯") ? "collins" :
                         header.Contains("俚语") ? "urban" : "";

            if (string.IsNullOrEmpty(key)) continue;

            var target = key switch
            {
                "oxford"  => ContentOxford,
                "collins" => ContentCollins,
                "urban"   => ContentUrban,
                _ => null
            };
            if (target == null) continue;

            var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var text = line.Trim();
                if (string.IsNullOrWhiteSpace(text)) continue;

                string? pos = null;
                string meaning = text;

                // 只有牛津和柯林斯才提取词性，俚语词典整行当释义
                if (key != "urban")
                {
                    var posMatch = Regex.Match(text, @"^(\w+\.?)\s+(.*)");
                    if (posMatch.Success && posMatch.Groups[1].Value.Length <= 8)
                    {
                        pos = posMatch.Groups[1].Value;
                        if (!pos.EndsWith('.')) pos += ".";
                        meaning = posMatch.Groups[2].Value;
                    }
                }

                var card = new Border
                {
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    BackgroundColor = Color.FromArgb("#252730"),
                    Stroke = Color.FromArgb("#22FFFFFF"),
                    StrokeThickness = 1,
                    Padding = new Thickness(14, 10),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var stack = new VerticalStackLayout { Spacing = 4 };
                if (pos != null)
                {
                    stack.Children.Add(new Label
                    {
                        Text = pos,
                        TextColor = Color.FromArgb("#F4A100"),
                        FontSize = 12,
                        FontAttributes = FontAttributes.Bold
                    });
                }
                stack.Children.Add(new Label
                {
                    Text = meaning,
                    TextColor = Color.FromArgb("#D1D5DB"),
                    FontSize = 14,
                    LineBreakMode = LineBreakMode.WordWrap
                });

                card.Content = stack;
                target.Children.Add(card);
            }
        }
    }
}
