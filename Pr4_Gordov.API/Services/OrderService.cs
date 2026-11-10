using Microsoft.EntityFrameworkCore;
using Pr4_Gordov.API.Data;
using Pr4_Gordov.API.DTO.Orders;
using Pr4_Gordov.API.Models;

namespace Pr4_Gordov.API.Services;

public class OrderService
{
    private readonly AppDbContext _context;
    private readonly CartService _cartService;

    public OrderService(AppDbContext context, CartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    public async Task<(bool Success, string? ErrorMessage, int? OrderId)> CreateOrderAsync(int userId, CreateOrderRequest request)
    {
        var itemsToOrder = new List<(Product Product, int Quantity)>();

        if (request.DirectProductId.HasValue)
        {
            int qty = request.DirectQuantity.GetValueOrDefault(1);
            if (qty <= 0) return (false, "Количество товара должно быть больше 0", null);

            var product = await _context.Products.FindAsync(request.DirectProductId.Value);
            if (product == null || !product.IsAvailable)
                return (false, "Выбранный товар недоступен для покупки", null);

            if (product.StockQuantity < qty)
                return (false, $"Недостаточно остатков на складе. В наличии: {product.StockQuantity}", null);

            itemsToOrder.Add((product, qty));
        }
        else
        {
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return (false, "Корзина пуста. Нечего оформлять", null);

            foreach (var ci in cartItems)
            {
                if (!ci.Product.IsAvailable || ci.Product.StockQuantity < ci.Quantity)
                {
                    return (false, $"Товар \"{ci.Product.Title}\" закончился или недоступен. Доступно: {ci.Product.StockQuantity}", null);
                }
                itemsToOrder.Add((ci.Product, ci.Quantity));
            }
        }

        decimal totalCost = itemsToOrder.Sum(i => i.Quantity * i.Product.Price);

        if (totalCost < 600m && request.DeliveryTypeId == 2)
        {
            return (false, "При сумме заказа менее 600 рублей доставка курьером недоступна. Выберите самовывоз из ПВЗ.", null);
        }

        if (request.DeliveryTypeId == 2 && string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            return (false, "Для курьерской доставки необходимо указать адрес доставки", null);
        }

        if (request.DeliveryTypeId == 1 && !request.PickupPointId.HasValue)
        {
            return (false, "Для самовывоза необходимо выбрать пункт выдачи заказов (ПВЗ)", null);
        }

        var order = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            StatusId = 1,
            DeliveryTypeId = request.DeliveryTypeId,
            DeliveryAddress = request.DeliveryTypeId == 2 ? request.DeliveryAddress : null,
            PickupPointId = request.DeliveryTypeId == 1 ? request.PickupPointId : null,
            TotalCost = totalCost
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        foreach (var (product, quantity) in itemsToOrder)
        {
            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.Price
            };
            _context.OrderItems.Add(orderItem);

            product.StockQuantity -= quantity;
        }

        if (!request.DirectProductId.HasValue)
        {
            await _cartService.ClearCartAsync(userId);
        }

        await _context.SaveChangesAsync();
        return (true, null, order.Id);
    }

    public async Task<List<OrderResponse>> GetUserOrdersAsync(int userId, string? sortBy = "newest")
    {
        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .Include(o => o.Status)
            .Include(o => o.DeliveryType)
            .Include(o => o.PickupPoint)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .AsQueryable();

        query = (sortBy?.ToLower() == "oldest")
            ? query.OrderBy(o => o.OrderDate)
            : query.OrderByDescending(o => o.OrderDate);

        return await query.Select(o => new OrderResponse(
            o.Id,
            o.OrderDate,
            o.Status.Name,
            o.DeliveryType.Name,
            o.DeliveryAddress,
            o.PickupPoint != null ? o.PickupPoint.Name : null,
            o.PickupPoint != null ? o.PickupPoint.Address : null,
            o.TotalCost,
            o.OrderItems.Select(oi => new OrderItemResponse(
                oi.Id,
                oi.ProductId,
                oi.Product.Title,
                oi.Product.ImagePath,
                oi.Quantity,
                oi.UnitPrice,
                oi.Quantity * oi.UnitPrice
            )).ToList()
        )).ToListAsync();
    }

    public async Task<OrderResponse?> GetOrderByIdAsync(int userId, int orderId, bool isAdmin = false)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId);

        if (!isAdmin)
        {
            query = query.Where(o => o.UserId == userId);
        }

        return await query
            .Include(o => o.Status)
            .Include(o => o.DeliveryType)
            .Include(o => o.PickupPoint)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Select(o => new OrderResponse(
                o.Id,
                o.OrderDate,
                o.Status.Name,
                o.DeliveryType.Name,
                o.DeliveryAddress,
                o.PickupPoint != null ? o.PickupPoint.Name : null,
                o.PickupPoint != null ? o.PickupPoint.Address : null,
                o.TotalCost,
                o.OrderItems.Select(oi => new OrderItemResponse(
                    oi.Id,
                    oi.ProductId,
                    oi.Product.Title,
                    oi.Product.ImagePath,
                    oi.Quantity,
                    oi.UnitPrice,
                    oi.Quantity * oi.UnitPrice
                )).ToList()
            )).FirstOrDefaultAsync();
    }

    public async Task<List<PickupPointResponse>> GetPickupPointsAsync()
    {
        return await _context.PickupPoints
            .AsNoTracking()
            .Select(p => new PickupPointResponse(p.Id, p.Name, p.Address, p.WorkingHours))
            .ToListAsync();
    }
}