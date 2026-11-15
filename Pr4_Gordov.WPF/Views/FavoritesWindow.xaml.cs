using System.Windows;
using System.Windows.Controls;
using Pr4_Gordov.WPF.Models;
using Pr4_Gordov.WPF.Services;

namespace Pr4_Gordov.WPF.Views;

public partial class FavoritesWindow : Window
{
    private readonly ApiClient _api;

    public FavoritesWindow(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        Loaded += FavoritesWindow_Loaded;
    }

    private async void FavoritesWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadFavoritesAsync();
    }

    private async Task LoadFavoritesAsync()
    {
        var list = await _api.GetFavoritesAsync();
        FavoritesGrid.ItemsSource = list;
    }

    private async void MoveToCart_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is FavoriteItemDto item)
        {
            var (success, error) = await _api.MoveFavoriteToCartAsync(item.ProductId);
            if (success)
            {
                MessageBox.Show($"Товар \"{item.ProductTitle}\" перемещен в корзину!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadFavoritesAsync();
            }
            else
            {
                MessageBox.Show(error ?? "Не удалось переместить в корзину", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async void Remove_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is FavoriteItemDto item)
        {
            await _api.RemoveFromFavoritesAsync(item.ProductId);
            await LoadFavoritesAsync();
        }
    }
}