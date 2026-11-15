using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Pr4_Gordov.WPF.Models;

namespace Pr4_Gordov.WPF.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public string? Token { get; private set; }
    public string? CurrentUserLogin { get; private set; }
    public string? CurrentUserRole { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
    public bool IsAdmin => CurrentUserRole?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true;

    public ApiClient(string baseAddress = "http://127.0.0.1:5213")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseAddress) };
    }

    public void SetToken(string token, string login, string role)
    {
        Token = token;
        CurrentUserLogin = login;
        CurrentUserRole = role;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public void Logout()
    {
        Token = null;
        CurrentUserLogin = null;
        CurrentUserRole = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response, string defaultMessage)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return "Требуется авторизация (сессия истекла или не выполнен вход)";
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            return "У вас нет прав для выполнения этой операции";

        try
        {
            var str = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(str)) return defaultMessage;

            using var doc = JsonDocument.Parse(str);
            if (doc.RootElement.TryGetProperty("message", out var msgProp))
            {
                var msg = msgProp.GetString();
                if (!string.IsNullOrWhiteSpace(msg)) return msg;
            }
            if (doc.RootElement.TryGetProperty("title", out var titleProp))
            {
                var title = titleProp.GetString();
                if (!string.IsNullOrWhiteSpace(title)) return title;
            }
            return str;
        }
        catch
        {
            return defaultMessage;
        }
    }

    public async Task<(bool Success, string? Error)> LoginAsync(string login, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/login", new { login, password });
            if (!response.IsSuccessStatusCode)
            {
                var err = await ExtractErrorMessageAsync(response, "Неверный логин или пароль");
                return (false, err);
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
            if (auth == null) return (false, "Ошибка ответа сервера");

            SetToken(auth.Token, auth.Login, auth.Role);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, "Не удалось подключиться к серверу API: " + ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(string login, string password, string confirmPassword)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/auth/register", new { login, password, confirmPassword });
            if (!response.IsSuccessStatusCode)
            {
                var err = await ExtractErrorMessageAsync(response, "Ошибка при регистрации");
                return (false, err);
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
            if (auth == null) return (false, "Ошибка ответа сервера");

            SetToken(auth.Token, auth.Login, auth.Role);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, "Не удалось подключиться к серверу API: " + ex.Message);
        }
    }

    public async Task<List<ProductDto>> GetProductsAsync(string? search = null, int? categoryId = null, decimal? minPrice = null, decimal? maxPrice = null, string? sortBy = null)
    {
        try
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
            if (categoryId.HasValue && categoryId.Value > 0) query.Add($"categoryId={categoryId.Value}");
            if (minPrice.HasValue) query.Add($"minPrice={minPrice.Value}");
            if (maxPrice.HasValue) query.Add($"maxPrice={maxPrice.Value}");
            if (!string.IsNullOrWhiteSpace(sortBy)) query.Add($"sortBy={sortBy}");

            string url = "/api/products" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
            var list = await _http.GetFromJsonAsync<List<ProductDto>>(url, _jsonOptions);
            return list ?? new List<ProductDto>();
        }
        catch
        {
            return new List<ProductDto>();
        }
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<CategoryDto>>("/api/admin/categories", _jsonOptions);
            return list ?? new List<CategoryDto>();
        }
        catch
        {
            return new List<CategoryDto>();
        }
    }

    public async Task<CartDto?> GetCartAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<CartDto>("/api/cart", _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool Success, string? Error)> AddToCartAsync(int productId, int quantity = 1)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/cart", new { productId, quantity });
            if (!response.IsSuccessStatusCode)
            {
                var err = await ExtractErrorMessageAsync(response, "Не удалось добавить товар в корзину");
                return (false, err);
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<bool> UpdateCartQuantityAsync(int cartItemId, int quantity)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/cart/{cartItemId}", new { quantity });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RemoveFromCartAsync(int cartItemId)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/cart/{cartItemId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<FavoriteItemDto>> GetFavoritesAsync()
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<FavoriteItemDto>>("/api/favorites", _jsonOptions);
            return list ?? new List<FavoriteItemDto>();
        }
        catch
        {
            return new List<FavoriteItemDto>();
        }
    }

    public async Task<(bool Success, string? Error)> AddToFavoritesAsync(int productId)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/favorites", new { productId });
            if (!response.IsSuccessStatusCode)
            {
                var err = await ExtractErrorMessageAsync(response, "Не удалось добавить в избранное");
                return (false, err);
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<bool> RemoveFromFavoritesAsync(int productId)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/favorites/{productId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, string? Error)> MoveFavoriteToCartAsync(int productId)
    {
        try
        {
            var response = await _http.PostAsync($"/api/favorites/{productId}/move-to-cart", null);
            if (!response.IsSuccessStatusCode)
            {
                var err = await ExtractErrorMessageAsync(response, "Не удалось перенести в корзину");
                return (false, err);
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<List<PickupPointDto>> GetPickupPointsAsync()
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<PickupPointDto>>("/api/pickup-points", _jsonOptions);
            return list ?? new List<PickupPointDto>();
        }
        catch
        {
            return new List<PickupPointDto>();
        }
    }

    public async Task<(bool Success, string? Error)> CreateOrderAsync(int deliveryTypeId, string? deliveryAddress, int? pickupPointId, int? directProductId = null, int? directQuantity = null)
    {
        try
        {
            var body = new
            {
                deliveryTypeId,
                deliveryAddress,
                pickupPointId,
                directProductId,
                directQuantity
            };
            var response = await _http.PostAsJsonAsync("/api/orders", body);
            if (!response.IsSuccessStatusCode)
            {
                var err = await ExtractErrorMessageAsync(response, "Не удалось оформить заказ");
                return (false, err);
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<List<OrderDto>> GetMyOrdersAsync(string? sortBy = "newest")
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<OrderDto>>($"/api/orders?sortBy={sortBy}", _jsonOptions);
            return list ?? new List<OrderDto>();
        }
        catch
        {
            return new List<OrderDto>();
        }
    }

    public async Task<List<ReviewDto>> GetReviewsByProductAsync(int productId)
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<ReviewDto>>($"/api/reviews/product/{productId}", _jsonOptions);
            return list ?? new List<ReviewDto>();
        }
        catch
        {
            return new List<ReviewDto>();
        }
    }

    public async Task<(bool Success, string? Error)> AddReviewAsync(int productId, int rating, string? comment)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/reviews", new { productId, rating, comment });
            if (!response.IsSuccessStatusCode)
            {
                var err = await ExtractErrorMessageAsync(response, "Не удалось опубликовать отзыв");
                return (false, err);
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<List<ProductDto>> GetAllProductsAdminAsync()
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<ProductDto>>("/api/admin/products", _jsonOptions);
            return list ?? new List<ProductDto>();
        }
        catch
        {
            return new List<ProductDto>();
        }
    }

    public async Task<(bool Success, string? Error)> CreateProductAdminAsync(UpsertProductDto product)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/admin/products", product);
            if (!response.IsSuccessStatusCode)
            {
                var err = await ExtractErrorMessageAsync(response, "Не удалось создать товар");
                return (false, err);
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> UpdateProductAdminAsync(int id, UpsertProductDto product)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/admin/products/{id}", product);
            if (!response.IsSuccessStatusCode)
            {
                var err = await ExtractErrorMessageAsync(response, "Не удалось обновить товар");
                return (false, err);
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> DeleteProductAdminAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/admin/products/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var err = await ExtractErrorMessageAsync(response, "Не удалось удалить товар");
                return (false, err);
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<bool> ToggleProductAvailabilityAdminAsync(int productId)
    {
        try
        {
            var response = await _http.PutAsync($"/api/admin/products/{productId}/toggle-availability", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<OrderDto>> GetAllOrdersAdminAsync()
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<OrderDto>>("/api/admin/orders", _jsonOptions);
            return list ?? new List<OrderDto>();
        }
        catch
        {
            return new List<OrderDto>();
        }
    }

    public async Task<bool> UpdateOrderStatusAdminAsync(int orderId, int statusId)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"/api/admin/orders/{orderId}/status", new { statusId });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<CategoryDto>> GetCategoriesAdminAsync()
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<CategoryDto>>("/api/admin/categories", _jsonOptions);
            return list ?? new List<CategoryDto>();
        }
        catch
        {
            return new List<CategoryDto>();
        }
    }

    public async Task<(bool Success, string? Error)> CreateCategoryAdminAsync(string name)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/admin/categories", new { name });
            if (!response.IsSuccessStatusCode)
                return (false, await ExtractErrorMessageAsync(response, "Не удалось создать категорию"));
            return (true, null);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> DeleteCategoryAdminAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/admin/categories/{id}");
            if (!response.IsSuccessStatusCode)
                return (false, await ExtractErrorMessageAsync(response, "Не удалось удалить категорию"));
            return (true, null);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<List<PickupPointDto>> GetPickupPointsAdminAsync()
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<PickupPointDto>>("/api/admin/pickup-points", _jsonOptions);
            return list ?? new List<PickupPointDto>();
        }
        catch
        {
            return new List<PickupPointDto>();
        }
    }

    public async Task<(bool Success, string? Error)> CreatePickupPointAdminAsync(string name, string address, string workingHours)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/api/admin/pickup-points", new { name, address, workingHours });
            if (!response.IsSuccessStatusCode)
                return (false, await ExtractErrorMessageAsync(response, "Не удалось создать ПВЗ"));
            return (true, null);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool Success, string? Error)> DeletePickupPointAdminAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/admin/pickup-points/{id}");
            if (!response.IsSuccessStatusCode)
                return (false, await ExtractErrorMessageAsync(response, "Не удалось удалить ПВЗ"));
            return (true, null);
        }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<List<ReviewDto>> GetAllReviewsAdminAsync()
    {
        try
        {
            var list = await _http.GetFromJsonAsync<List<ReviewDto>>("/api/admin/reviews", _jsonOptions);
            return list ?? new List<ReviewDto>();
        }
        catch
        {
            return new List<ReviewDto>();
        }
    }

    public async Task<bool> DeleteReviewAdminAsync(int id)
    {
        try
        {
            var response = await _http.DeleteAsync($"/api/admin/reviews/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}