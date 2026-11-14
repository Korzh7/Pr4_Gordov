using System.Windows;
using System.Windows.Controls;
using Pr4_Gordov.WPF.Models;
using Pr4_Gordov.WPF.Services;

namespace Pr4_Gordov.WPF.Views;

public partial class OrdersWindow : Window
{
    private readonly ApiClient _api;

    public OrdersWindow(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        Loaded += OrdersWindow_Loaded;
    }

    private async void OrdersWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadOrdersAsync();
    }

    private async Task LoadOrdersAsync()
    {
        string sort = SortCombo.SelectedIndex == 1 ? "oldest" : "newest";
        var orders = await _api.GetMyOrdersAsync(sort);

        var displayOrders = orders.Select(o => new
        {
            o.Id,
            o.OrderDate,
            o.Status,
            o.DeliveryType,
            DeliveryAddress = !string.IsNullOrEmpty(o.PickupPointName)
                ? $"{o.PickupPointName} ({o.PickupPointAddress})"
                : o.DeliveryAddress,
            o.TotalCost,
            o.Items
        }).ToList();

        OrdersGrid.ItemsSource = displayOrders;
        OrderItemsGrid.ItemsSource = null;
    }

    private async void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
        {
            await LoadOrdersAsync();
        }
    }

    private void OrdersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OrdersGrid.SelectedItem != null)
        {
            dynamic selected = OrdersGrid.SelectedItem;
            OrderItemsGrid.ItemsSource = selected.Items;
        }
        else
        {
            OrderItemsGrid.ItemsSource = null;
        }
    }
}