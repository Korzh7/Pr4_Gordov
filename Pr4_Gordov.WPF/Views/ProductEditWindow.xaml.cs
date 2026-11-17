using System.Windows;
using Pr4_Gordov.WPF.Models;
using Pr4_Gordov.WPF.Services;

namespace Pr4_Gordov.WPF.Views;

public partial class ProductEditWindow : Window
{
    private readonly ApiClient _api;
    private readonly ProductDto? _productToEdit;
    private readonly List<CategoryDto> _categories;

    public bool IsSaved { get; private set; }

    public ProductEditWindow(ApiClient api, List<CategoryDto> categories, ProductDto? productToEdit = null)
    {
        InitializeComponent();
        _api = api;
        _categories = categories;
        _productToEdit = productToEdit;

        CategoryCombo.ItemsSource = _categories;

        if (_productToEdit != null)
        {
            HeaderTitleText.Text = "Редактирование товара";
            Title = "Редактирование товара";

            TitleBox.Text = _productToEdit.Title;
            CategoryCombo.SelectedValue = _productToEdit.CategoryId;
            PriceBox.Text = _productToEdit.Price.ToString("0.00");
            StockBox.Text = _productToEdit.StockQuantity.ToString();
            PackageBox.Text = _productToEdit.PackageQuantity?.ToString() ?? "";
            ImageBox.Text = _productToEdit.ImagePath ?? "";
            DescriptionBox.Text = _productToEdit.Description ?? "";
            IsAvailableCheck.IsChecked = _productToEdit.IsAvailable;
        }
        else
        {
            HeaderTitleText.Text = "Добавление нового товара";
            Title = "Новый товар";
            if (_categories.Count > 0)
                CategoryCombo.SelectedIndex = 0;
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        string title = TitleBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            ShowError("Введите название товара");
            return;
        }

        if (CategoryCombo.SelectedValue is not int categoryId)
        {
            ShowError("Выберите категорию");
            return;
        }

        if (!decimal.TryParse(PriceBox.Text.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture, out decimal price) || price <= 0)
        {
            ShowError("Введите корректную цену товара (больше 0)");
            return;
        }

        if (!int.TryParse(StockBox.Text.Trim(), out int stock) || stock < 0)
        {
            ShowError("Введите количество товара на складе (0 или больше)");
            return;
        }

        int? package = int.TryParse(PackageBox.Text.Trim(), out int pkg) && pkg > 0 ? pkg : null;
        string? image = string.IsNullOrWhiteSpace(ImageBox.Text) ? null : ImageBox.Text.Trim();
        string? desc = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim();
        bool isAvailable = IsAvailableCheck.IsChecked == true;

        var dto = new UpsertProductDto(title, desc, price, categoryId, image, package, stock, isAvailable);

        if (_productToEdit == null)
        {
            var (success, error) = await _api.CreateProductAdminAsync(dto);
            if (success)
            {
                IsSaved = true;
                Close();
            }
            else
            {
                ShowError(error ?? "Не удалось создать товар");
            }
        }
        else
        {
            var (success, error) = await _api.UpdateProductAdminAsync(_productToEdit.Id, dto);
            if (success)
            {
                IsSaved = true;
                Close();
            }
            else
            {
                ShowError(error ?? "Не удалось обновить товар");
            }
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorText.Visibility = Visibility.Visible;
    }
}