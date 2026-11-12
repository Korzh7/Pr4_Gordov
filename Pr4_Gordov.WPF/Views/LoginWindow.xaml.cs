using System.Windows;
using Pr4_Gordov.WPF.Services;

namespace Pr4_Gordov.WPF.Views;

public partial class LoginWindow : Window
{
    private readonly ApiClient _api;
    private bool _isRegisterMode = false;

    public LoginWindow(ApiClient api)
    {
        InitializeComponent();
        _api = api;
    }

    private void RegisterToggleBtn_Click(object sender, RoutedEventArgs e)
    {
        _isRegisterMode = !_isRegisterMode;
        if (_isRegisterMode)
        {
            ConfirmPasswordLabel.Visibility = Visibility.Visible;
            ConfirmPasswordBox.Visibility = Visibility.Visible;
            LoginBtn.Content = "Зарегистрироваться";
            RegisterToggleBtn.Content = "Уже есть аккаунт? Войти";
        }
        else
        {
            ConfirmPasswordLabel.Visibility = Visibility.Collapsed;
            ConfirmPasswordBox.Visibility = Visibility.Collapsed;
            LoginBtn.Content = "Войти";
            RegisterToggleBtn.Content = "Нет аккаунта? Зарегистрироваться";
        }
        ErrorText.Visibility = Visibility.Collapsed;
    }

    private async void LoginBtn_Click(object sender, RoutedEventArgs e)
    {
        string login = LoginBox.Text.Trim();
        string pass = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(pass))
        {
            ShowError("Заполните логин и пароль");
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;
        LoginBtn.IsEnabled = false;

        if (_isRegisterMode)
        {
            string confirm = ConfirmPasswordBox.Password;
            if (pass != confirm)
            {
                ShowError("Пароли не совпадают!");
                LoginBtn.IsEnabled = true;
                return;
            }

            var (success, error) = await _api.RegisterAsync(login, pass, confirm);
            if (!success)
            {
                ShowError(error ?? "Ошибка при регистрации");
                LoginBtn.IsEnabled = true;
                return;
            }
        }
        else
        {
            var (success, error) = await _api.LoginAsync(login, pass);
            if (!success)
            {
                ShowError(error ?? "Неверный логин или пароль");
                LoginBtn.IsEnabled = true;
                return;
            }
        }

        OpenMainWindow();
    }

    private void GuestBtn_Click(object sender, RoutedEventArgs e)
    {
        _api.Logout();
        OpenMainWindow();
    }

    private void OpenMainWindow()
    {
        var main = new MainWindow(_api);
        main.Show();
        Close();
    }

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorText.Visibility = Visibility.Visible;
    }
}