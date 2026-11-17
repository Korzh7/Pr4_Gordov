using System.Windows;
using System.Windows.Controls;
using Pr4_Gordov.WPF.Models;
using Pr4_Gordov.WPF.Services;

namespace Pr4_Gordov.WPF.Views;

public partial class AdminWindow : Window
{
    private readonly ApiClient _api;
    private List<CategoryDto> _categories = new();
    private List<ProductDto> _products = new();

    public AdminWindow(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        Loaded += AdminWindow_Loaded;
    }

    private async void AdminWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAllAsync();
    }

    private async Task LoadAllAsync()
    {
        await LoadProductsAsync();
        await LoadOrdersAsync();
        await LoadCategoriesAsync();
        await LoadPickupPointsAsync();
        await LoadReviewsAsync();
    }

    private async Task LoadProductsAsync()
    {
        _products = await _api.GetAllProductsAdminAsync();
        AdminProductsGrid.ItemsSource = _products;
    }

    private async void AddProduct_Click(object sender, RoutedEventArgs e)
    {
        var editWin = new ProductEditWindow(_api, _categories) { Owner = this };
        editWin.ShowDialog();
        if (editWin.IsSaved)
        {
            await LoadProductsAsync();
        }
    }

    private async void EditProduct_Click(object sender, RoutedEventArgs e)
    {
        if (AdminProductsGrid.SelectedItem is not ProductDto selected)
        {
            MessageBox.Show("Выберите товар для редактирования в таблице", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var editWin = new ProductEditWindow(_api, _categories, selected) { Owner = this };
        editWin.ShowDialog();
        if (editWin.IsSaved)
        {
            await LoadProductsAsync();
        }
    }

    private async void ToggleAvailability_Click(object sender, RoutedEventArgs e)
    {
        if (AdminProductsGrid.SelectedItem is not ProductDto selected)
        {
            MessageBox.Show("Выберите товар в таблице", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool ok = await _api.ToggleProductAvailabilityAdminAsync(selected.Id);
        if (ok)
        {
            await LoadProductsAsync();
        }
        else
        {
            MessageBox.Show("Не удалось изменить статус доступности", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
    {
        if (AdminProductsGrid.SelectedItem is not ProductDto selected)
        {
            MessageBox.Show("Выберите товар для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var res = MessageBox.Show($"Вы уверены, что хотите удалить товар \"{selected.Title}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res == MessageBoxResult.Yes)
        {
            var (success, error) = await _api.DeleteProductAdminAsync(selected.Id);
            if (success)
            {
                await LoadProductsAsync();
            }
            else
            {
                MessageBox.Show(error ?? "Не удалось удалить товар", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void RefreshProducts_Click(object sender, RoutedEventArgs e)
    {
        await LoadProductsAsync();
    }

    private async Task LoadOrdersAsync()
    {
        var orders = await _api.GetAllOrdersAdminAsync();
        var displayOrders = orders.Select(o => new
        {
            o.Id,
            o.OrderDate,
            o.Status,
            o.DeliveryType,
            DeliveryAddress = !string.IsNullOrEmpty(o.PickupPointName)
                ? $"{o.PickupPointName} ({o.PickupPointAddress})"
                : o.DeliveryAddress,
            o.TotalCost
        }).ToList();
        AdminOrdersGrid.ItemsSource = displayOrders;
    }

    private async void ChangeStatusBtn_Click(object sender, RoutedEventArgs e)
    {
        if (AdminOrdersGrid.SelectedItem == null)
        {
            MessageBox.Show("Выберите заказ в таблице", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        dynamic selected = AdminOrdersGrid.SelectedItem;
        int orderId = selected.Id;

        if (StatusCombo.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out int statusId))
        {
            bool ok = await _api.UpdateOrderStatusAdminAsync(orderId, statusId);
            if (ok)
            {
                MessageBox.Show("Статус заказа успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadOrdersAsync();
            }
            else
            {
                MessageBox.Show("Не удалось изменить статус", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task LoadCategoriesAsync()
    {
        _categories = await _api.GetCategoriesAdminAsync();
        CategoriesGrid.ItemsSource = _categories;
    }

    private async void AddCategory_Click(object sender, RoutedEventArgs e)
    {
        string name = NewCategoryNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Введите название категории", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var (success, error) = await _api.CreateCategoryAdminAsync(name);
        if (success)
        {
            NewCategoryNameBox.Text = "";
            await LoadCategoriesAsync();
        }
        else
        {
            MessageBox.Show(error ?? "Не удалось создать категорию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
    {
        if (CategoriesGrid.SelectedItem is not CategoryDto selected)
        {
            MessageBox.Show("Выберите категорию для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var (success, error) = await _api.DeleteCategoryAdminAsync(selected.Id);
        if (success)
        {
            await LoadCategoriesAsync();
        }
        else
        {
            MessageBox.Show(error ?? "Не удалось удалить категорию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadPickupPointsAsync()
    {
        var pvzList = await _api.GetPickupPointsAdminAsync();
        PickupPointsGrid.ItemsSource = pvzList;
    }

    private async void AddPvz_Click(object sender, RoutedEventArgs e)
    {
        string name = PvzNameBox.Text.Trim();
        string address = PvzAddressBox.Text.Trim();
        string hours = PvzHoursBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(address))
        {
            MessageBox.Show("Заполните название и адрес ПВЗ", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var (success, error) = await _api.CreatePickupPointAdminAsync(name, address, hours);
        if (success)
        {
            PvzNameBox.Text = "";
            PvzAddressBox.Text = "";
            await LoadPickupPointsAsync();
        }
        else
        {
            MessageBox.Show(error ?? "Не удалось создать ПВЗ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void DeletePvz_Click(object sender, RoutedEventArgs e)
    {
        if (PickupPointsGrid.SelectedItem is not PickupPointDto selected)
        {
            MessageBox.Show("Выберите ПВЗ для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var (success, error) = await _api.DeletePickupPointAdminAsync(selected.Id);
        if (success)
        {
            await LoadPickupPointsAsync();
        }
        else
        {
            MessageBox.Show(error ?? "Не удалось удалить ПВЗ", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadReviewsAsync()
    {
        var reviews = await _api.GetAllReviewsAdminAsync();
        ReviewsGrid.ItemsSource = reviews;
    }

    private async void DeleteReview_Click(object sender, RoutedEventArgs e)
    {
        if (ReviewsGrid.SelectedItem is not ReviewDto selected)
        {
            MessageBox.Show("Выберите отзыв для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var res = MessageBox.Show($"Удалить отзыв от пользователя \"{selected.UserLogin}\"?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res == MessageBoxResult.Yes)
        {
            bool ok = await _api.DeleteReviewAdminAsync(selected.Id);
            if (ok)
            {
                await LoadReviewsAsync();
            }
            else
            {
                MessageBox.Show("Не удалось удалить отзыв", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}