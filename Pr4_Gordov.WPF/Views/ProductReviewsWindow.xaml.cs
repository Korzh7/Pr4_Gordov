using System.Windows;
using System.Windows.Controls;
using Pr4_Gordov.WPF.Models;
using Pr4_Gordov.WPF.Services;

namespace Pr4_Gordov.WPF.Views;

public partial class ProductReviewsWindow : Window
{
    private readonly ApiClient _api;
    private readonly ProductDto _product;

    public ProductReviewsWindow(ApiClient api, ProductDto product)
    {
        InitializeComponent();
        _api = api;
        _product = product;
        Loaded += ProductReviewsWindow_Loaded;
    }

    private async void ProductReviewsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ProductTitleText.Text = _product.Title;
        ProductRatingText.Text = $"⭐ Средняя оценка: {_product.Rating:0.0}";

        if (!_api.IsAuthenticated)
        {
            AddReviewBorder.Visibility = Visibility.Collapsed;
        }

        await LoadReviewsAsync();
    }

    private async Task LoadReviewsAsync()
    {
        var reviews = await _api.GetReviewsByProductAsync(_product.Id);
        ReviewsList.ItemsSource = reviews;
        if (reviews.Count > 0)
        {
            double avg = reviews.Average(r => r.Rating);
            ProductRatingText.Text = $"⭐ Средняя оценка: {avg:0.0} (отзывов: {reviews.Count})";
        }
        else
        {
            ProductRatingText.Text = "Отзывов пока нет. Будьте первым!";
        }
    }

    private async void SubmitReview_Click(object sender, RoutedEventArgs e)
    {
        ReviewErrorText.Visibility = Visibility.Collapsed;

        int rating = 5;
        if (RatingCombo.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out int r))
        {
            rating = r;
        }

        string? comment = string.IsNullOrWhiteSpace(CommentBox.Text) ? null : CommentBox.Text.Trim();

        var (success, error) = await _api.AddReviewAsync(_product.Id, rating, comment);
        if (success)
        {
            MessageBox.Show("Ваш отзыв успешно опубликован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            CommentBox.Text = "";
            await LoadReviewsAsync();
        }
        else
        {
            ReviewErrorText.Text = error ?? "Не удалось отправить отзыв";
            ReviewErrorText.Visibility = Visibility.Visible;
        }
    }
}