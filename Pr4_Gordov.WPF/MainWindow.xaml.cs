using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Pr4_Gordov.WPF.Models;
using Pr4_Gordov.WPF.Services;
using Pr4_Gordov.WPF.Views;

namespace Pr4_Gordov.WPF;

public partial class MainWindow : Window
{
    private readonly ApiClient _api;
    private List<CategoryDto> _categories = new();
    private readonly DispatcherTimer _debounceTimer;
    private bool _isLoaded = false;

    public MainWindow(ApiClient? api = null)
    {
        _api = api ?? new ApiClient();

        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _debounceTimer.Tick += DebounceTimer_Tick;

        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateUserHeader();
        await LoadCategoriesAsync();
        await LoadProductsAsync();
        _isLoaded = true;
    }

    private void UpdateUserHeader()
    {
        if (_api.IsAuthenticated)
        {
            UserInfoText.Text = $"Пользователь: {_api.CurrentUserLogin} ({_api.CurrentUserRole})";
            AuthBtn.Content = "Выйти";
            OrdersBtn.Visibility = Visibility.Visible;
            FavoritesBtn.Visibility = Visibility.Visible;
            CartBtn.Visibility = Visibility.Visible;
            AdminBtn.Visibility = _api.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            UserInfoText.Text = "Гость (без входа)";
            AuthBtn.Content = "Войти";
            OrdersBtn.Visibility = Visibility.Collapsed;
            AdminBtn.Visibility = Visibility.Collapsed;
        }
    }

    private async Task LoadCategoriesAsync()
    {
        _categories = await _api.GetCategoriesAsync();
        var allCats = new List<CategoryDto> { new(0, "Все категории") };
        allCats.AddRange(_categories);
        CategoryCombo.ItemsSource = allCats;
        CategoryCombo.SelectedIndex = 0;
    }

    private async Task LoadProductsAsync()
    {
        if (SearchBox == null || CategoryCombo == null || MinPriceBox == null || MaxPriceBox == null || SortCombo == null)
            return;

        string? search = string.IsNullOrWhiteSpace(SearchBox.Text) ? null : SearchBox.Text.Trim();
        int? categoryId = (CategoryCombo.SelectedValue as int?) > 0 ? (int?)CategoryCombo.SelectedValue : null;

        decimal? minPrice = decimal.TryParse(MinPriceBox.Text.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture, out var min) ? min : null;
        decimal? maxPrice = decimal.TryParse(MaxPriceBox.Text.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture, out var max) ? max : null;

        string? sortBy = (SortCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();

        var products = await _api.GetProductsAsync(search, categoryId, minPrice, maxPrice, sortBy);
        ProductsList.ItemsSource = products;
    }

    private void DynamicFilter_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded || _debounceTimer == null) return;
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private async void DebounceTimer_Tick(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();
        await LoadProductsAsync();
    }

    private async void ResetFilter_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";
        MinPriceBox.Text = "";
        MaxPriceBox.Text = "";
        if (CategoryCombo.Items.Count > 0) CategoryCombo.SelectedIndex = 0;
        if (SortCombo.Items.Count > 0) SortCombo.SelectedIndex = 0;
        await LoadProductsAsync();
    }

    private async void AddToCart_Click(object sender, RoutedEventArgs e)
    {
        if (!_api.IsAuthenticated)
        {
            AskToLogin();
            return;
        }

        if ((sender as Button)?.DataContext is ProductDto product)
        {
            var (success, error) = await _api.AddToCartAsync(product.Id, 1);
            if (success)
            {
                MessageBox.Show($"Товар \"{product.Title}\" добавлен в корзину!", "Корзина", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                string msg = string.IsNullOrWhiteSpace(error) ? "Не удалось добавить товар в корзину" : error;
                MessageBox.Show(msg, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async void BuyDirect_Click(object sender, RoutedEventArgs e)
    {
        if (!_api.IsAuthenticated)
        {
            AskToLogin();
            return;
        }

        if ((sender as Button)?.DataContext is ProductDto product)
        {
            var pvzList = await _api.GetPickupPointsAsync();
            if (pvzList.Count == 0)
            {
                MessageBox.Show("Нет доступных пунктов выдачи (ПВЗ) для оформления покупки", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var res = MessageBox.Show($"Оформить быструю покупку \"{product.Title}\" за {product.Price:N2} ₽ с получением в \"{pvzList[0].Name}\"?",
                "Покупка в 1 клик", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                var (success, error) = await _api.CreateOrderAsync(1, null, pvzList[0].Id, directProductId: product.Id, directQuantity: 1);
                if (success)
                {
                    MessageBox.Show("Заказ успешно оформлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadProductsAsync();
                }
                else
                {
                    string msg = string.IsNullOrWhiteSpace(error) ? "Ошибка оформления заказа" : error;
                    MessageBox.Show(msg, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private async void AddToFavorites_Click(object sender, RoutedEventArgs e)
    {
        if (!_api.IsAuthenticated)
        {
            AskToLogin();
            return;
        }

        if ((sender as Button)?.DataContext is ProductDto product)
        {
            var (success, error) = await _api.AddToFavoritesAsync(product.Id);
            if (success)
            {
                MessageBox.Show($"Товар \"{product.Title}\" добавлен в избранное!", "Избранное", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                string msg = string.IsNullOrWhiteSpace(error) ? "Не удалось добавить в избранное" : error;
                MessageBox.Show(msg, "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async void ProductReviews_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is ProductDto product)
        {
            var reviewsWin = new ProductReviewsWindow(_api, product) { Owner = this };
            reviewsWin.ShowDialog();
            await LoadProductsAsync();
        }
    }

    private async void FavoritesBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!_api.IsAuthenticated)
        {
            AskToLogin();
            return;
        }

        var favWin = new FavoritesWindow(_api) { Owner = this };
        favWin.ShowDialog();
        await LoadProductsAsync();
    }

    private async void CartBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!_api.IsAuthenticated)
        {
            AskToLogin();
            return;
        }

        var cartWin = new CartWindow(_api) { Owner = this };
        cartWin.ShowDialog();
        await LoadProductsAsync();
    }

    private void OrdersBtn_Click(object sender, RoutedEventArgs e)
    {
        var ordersWin = new OrdersWindow(_api) { Owner = this };
        ordersWin.ShowDialog();
    }

    private async void AdminBtn_Click(object sender, RoutedEventArgs e)
    {
        var adminWin = new AdminWindow(_api) { Owner = this };
        adminWin.ShowDialog();
        await LoadProductsAsync();
    }

    private void AuthBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_api.IsAuthenticated)
        {
            _api.Logout();
            UpdateUserHeader();
        }
        else
        {
            var loginWin = new LoginWindow(_api);
            loginWin.Show();
            Close();
        }
    }

    private void AskToLogin()
    {
        var res = MessageBox.Show("Для этого действия необходимо войти в аккаунт. Перейти к окну входа?", "Требуется авторизация", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res == MessageBoxResult.Yes)
        {
            var loginWin = new LoginWindow(_api);
            loginWin.Show();
            Close();
        }
    }
}
