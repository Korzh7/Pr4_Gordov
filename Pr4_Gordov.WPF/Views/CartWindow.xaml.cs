using System.Windows;
using System.Windows.Controls;
using Pr4_Gordov.WPF.Models;
using Pr4_Gordov.WPF.Services;

namespace Pr4_Gordov.WPF.Views;

public partial class CartWindow : Window
{
    private readonly ApiClient _api;
    private CartDto? _cart;
    private List<PickupPointDto> _pvzList = new();

    public CartWindow(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        Loaded += CartWindow_Loaded;
    }

    private async void CartWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadPvzAsync();
        await RefreshCartAsync();
    }

    private async Task LoadPvzAsync()
    {
        _pvzList = await _api.GetPickupPointsAsync();
        PvzCombo.ItemsSource = _pvzList;
        if (_pvzList.Count > 0)
            PvzCombo.SelectedIndex = 0;
    }

    private async Task RefreshCartAsync()
    {
        _cart = await _api.GetCartAsync();
        CartGrid.ItemsSource = _cart?.Items;

        decimal total = _cart?.TotalAmount ?? 0m;
        TotalCostText.Text = $"Итого: {total:N2} ₽";

        if (total < 600m)
        {
            CourierRadio.IsEnabled = false;
            CourierRadio.ToolTip = "Курьерская доставка доступна только при сумме от 600 ₽";
            PickupRadio.IsChecked = true;
        }
        else
        {
            CourierRadio.IsEnabled = true;
            CourierRadio.ToolTip = null;
        }

        CheckoutBtn.IsEnabled = _cart != null && _cart.Items.Count > 0;
    }

    private void DeliveryRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (PvzPanel == null || AddressPanel == null) return;

        if (PickupRadio.IsChecked == true)
        {
            PvzPanel.Visibility = Visibility.Visible;
            AddressPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            PvzPanel.Visibility = Visibility.Collapsed;
            AddressPanel.Visibility = Visibility.Visible;
        }
    }

    private async void DecreaseQty_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is CartItemDto item)
        {
            if (item.Quantity > 1)
            {
                await _api.UpdateCartQuantityAsync(item.Id, item.Quantity - 1);
            }
            else
            {
                await _api.RemoveFromCartAsync(item.Id);
            }
            await RefreshCartAsync();
        }
    }

    private async void IncreaseQty_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is CartItemDto item)
        {
            if (item.Quantity < item.StockQuantity)
            {
                await _api.UpdateCartQuantityAsync(item.Id, item.Quantity + 1);
                await RefreshCartAsync();
            }
            else
            {
                MessageBox.Show($"Нельзя добавить больше: на складе всего {item.StockQuantity} шт.", "Ограничение склада", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private async void RemoveItem_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is CartItemDto item)
        {
            await _api.RemoveFromCartAsync(item.Id);
            await RefreshCartAsync();
        }
    }

    private async void CheckoutBtn_Click(object sender, RoutedEventArgs e)
    {
        OrderErrorText.Visibility = Visibility.Collapsed;

        int deliveryTypeId = PickupRadio.IsChecked == true ? 1 : 2;
        int? pvzId = deliveryTypeId == 1 ? (int?)PvzCombo.SelectedValue : null;
        string? address = deliveryTypeId == 2 ? AddressBox.Text.Trim() : null;

        if (deliveryTypeId == 1 && pvzId == null)
        {
            OrderErrorText.Text = "Выберите пункт выдачи!";
            OrderErrorText.Visibility = Visibility.Visible;
            return;
        }

        if (deliveryTypeId == 2 && string.IsNullOrWhiteSpace(address))
        {
            OrderErrorText.Text = "Введите адрес доставки!";
            OrderErrorText.Visibility = Visibility.Visible;
            return;
        }

        CheckoutBtn.IsEnabled = false;
        var (success, error) = await _api.CreateOrderAsync(deliveryTypeId, address, pvzId);

        if (success)
        {
            MessageBox.Show("Заказ успешно оформлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        else
        {
            OrderErrorText.Text = error ?? "Ошибка при оформлении заказа";
            OrderErrorText.Visibility = Visibility.Visible;
            CheckoutBtn.IsEnabled = true;
        }
    }
}