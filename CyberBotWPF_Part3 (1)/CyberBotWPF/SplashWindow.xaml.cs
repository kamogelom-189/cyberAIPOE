using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CyberBotWPF;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            VoiceService.Play();
            NameBox.Focus();
        };
    }

    private void StartButton_Click(object sender, RoutedEventArgs e) => TryStart();

    private void NameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) TryStart();
    }

    private void TryStart()
    {
        string name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ErrorLabel.Visibility = Visibility.Visible;
            NameBox.Focus();
            return;
        }

        name = new string(name.Where(c => c >= 32 && c < 127).ToArray());

        if (name.Length > 30)
            name = name.Substring(0, 30);

        if (name.Length == 0)
            name = "User";

        var main = new MainWindow(name);
        main.Show();
        Close();
    }
}