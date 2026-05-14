using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Cart;
using Platform.Application.Catalog.Markets;
using Platform.Application.Catalog.Products;
using Platform.Application.Catalog.Variants;
using Platform.Application.Companies;
using Platform.Application.Customers;
using Platform.Application.Orders;
using Platform.Application.Orders.Commands;
using Platform.Application.Orders.Queries;
using Platform.Contracts.Common;
using Platform.Contracts.Orders;
using Platform.Domain.Orders;
using CartDomain = Platform.Domain.Cart;
using CartAdminApplicationService = Platform.Infrastructure.Cart.CartAdminApplicationService;

namespace Platform.Infrastructure.Orders;

public sealed class OrderAdminApplicationService : IOrderAdminApplicationService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IMarketRepository _marketRepository;
    private readonly CartAdminApplicationService _cartPricingService;
    private readonly IUnitOfWork _unitOfWork;

    public OrderAdminApplicationService(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        ICompanyRepository companyRepository,
        ICustomerRepository customerRepository,
        IMarketRepository marketRepository,
        CartAdminApplicationService cartPricingService,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _companyRepository = companyRepository;
        _customerRepository = customerRepository;
        _marketRepository = marketRepository;
        _cartPricingService = cartPricingService;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResponse<OrderSummaryDto>> ListAsync(ListOrdersQuery query, CancellationToken cancellationToken)
    {
        var result = await _orderRepository.ListAsync(query, cancellationToken);
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;

        return new PagedResponse<OrderSummaryDto>(
            result.Items.Select(MapSummary).ToList(),
            result.Total,
            page,
            pageSize);
    }

    public async Task<OrderDetailsDto?> GetByIdAsync(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);
        return order is null ? null : MapDetails(order);
    }

    public async Task<IReadOnlyList<OrderStatusHistoryDto>?> GetStatusHistoryAsync(GetOrderStatusHistoryQuery query, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(query.OrderId, cancellationToken);
        return order?.StatusHistory
            .OrderByDescending(x => x.ChangedAtUtc)
            .Select(MapStatusHistory)
            .ToList();
    }

    public async Task<OrderDetailsDto> CreateAsync(CreateOrderCommand command, string requestedBy, CancellationToken cancellationToken)
    {
        if (command.CartId.HasValue)
        {
            return await CreateFromCartAsync(command, requestedBy, cancellationToken);
        }

        return await CreateManualAsync(command, requestedBy, cancellationToken);
    }

    public async Task<OrderStatusHistoryDto?> ChangeStatusAsync(ChangeOrderStatusCommand command, string changedBy, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        OrderStatusHistory history;
        try
        {
            history = order.ChangeStatus(command.ToStatus, changedBy, command.Comment, command.RowVersion);
        }
        catch (InvalidOperationException exception)
        {
            throw new RequestValidationException(nameof(ChangeOrderStatusCommand.ToStatus), exception.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapStatusHistory(history);
    }

    public async Task<PaymentTransactionDto?> AddPaymentTransactionAsync(AddPaymentTransactionCommand command, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var transaction = order.AddPaymentTransaction(
            command.Provider,
            command.ProviderReference,
            command.Type,
            command.Status,
            command.Amount,
            command.CurrencyCode,
            command.RequestedAtUtc == default ? DateTime.UtcNow : command.RequestedAtUtc,
            command.CompletedAtUtc,
            command.RowVersion);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapPaymentTransaction(transaction);
    }

    private async Task<OrderDetailsDto> CreateFromCartAsync(CreateOrderCommand command, string requestedBy, CancellationToken cancellationToken)
    {
        var existing = await _orderRepository.GetBySourceCartIdAsync(command.CartId!.Value, cancellationToken);
        if (existing is not null)
        {
            return MapDetails(existing);
        }

        var cart = await _cartRepository.GetByIdAsync(command.CartId.Value, cancellationToken)
            ?? throw new RequestValidationException(nameof(CreateOrderCommand.CartId), "Unknown cart.");

        if (!string.Equals(cart.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            throw new RequestValidationException(nameof(CreateOrderCommand.CartId), "Only active carts can be converted into orders.");
        }

        if (string.IsNullOrWhiteSpace(command.CartRowVersion))
        {
            throw new RequestValidationException(nameof(CreateOrderCommand.CartRowVersion), "Cart row version is required when converting a cart.");
        }

        await EnsureCompanyMembershipCanPlaceOrdersAsync(cart.CustomerId, cart.CompanyId, cancellationToken);

        var updates = await _cartPricingService.ResolveCartPricingAsync(cart, cancellationToken);
        cart.Reprice(updates, command.CartRowVersion);

        var orderId = Guid.NewGuid();

        var order = new Order(
            orderId,
            cart.Id,
            await GenerateOrderNumberAsync(cancellationToken),
            cart.CustomerId,
            cart.CompanyId,
            cart.MarketId,
            cart.CurrencyCode,
            cart.CultureCode,
            cart.Email ?? string.Empty,
            DateTime.UtcNow,
            cart.Lines.Select(x => new OrderLine(Guid.NewGuid(), orderId, x.VariantId, x.Sku, x.ProductName, x.VariantDescription, x.Quantity, x.UnitPrice, x.VatRate)).ToList(),
            cart.Addresses.Select(x => new OrderAddress(Guid.NewGuid(), orderId, x.Type, x.FirstName, x.LastName, x.CompanyName, x.Line1, x.Line2, x.PostalCode, x.City, x.Region, x.CountryCode, x.Email, x.Phone)).ToList(),
            requestedBy,
            $"Created from cart {cart.Id}.");

        cart.Convert(cart.RowVersion);
        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(order);
    }

    private async Task<OrderDetailsDto> CreateManualAsync(CreateOrderCommand command, string requestedBy, CancellationToken cancellationToken)
    {
        if (!command.MarketId.HasValue)
        {
            throw new RequestValidationException(nameof(CreateOrderCommand.MarketId), "MarketId is required for manual orders.");
        }

        if (string.IsNullOrWhiteSpace(command.CurrencyCode))
        {
            throw new RequestValidationException(nameof(CreateOrderCommand.CurrencyCode), "CurrencyCode is required for manual orders.");
        }

        if (string.IsNullOrWhiteSpace(command.CultureCode))
        {
            throw new RequestValidationException(nameof(CreateOrderCommand.CultureCode), "CultureCode is required for manual orders.");
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            throw new RequestValidationException(nameof(CreateOrderCommand.Email), "Email is required for manual orders.");
        }

        if (command.Lines.Count == 0)
        {
            throw new RequestValidationException(nameof(CreateOrderCommand.Lines), "Manual order must contain at least one line.");
        }

        if (await _marketRepository.GetByIdAsync(command.MarketId.Value, cancellationToken) is null)
        {
            throw new RequestValidationException(nameof(CreateOrderCommand.MarketId), "Unknown market.");
        }

        if (command.CustomerId.HasValue && await _customerRepository.GetByIdAsync(command.CustomerId.Value, cancellationToken) is null)
        {
            throw new RequestValidationException(nameof(CreateOrderCommand.CustomerId), "Unknown customer.");
        }

        await EnsureCompanyMembershipCanPlaceOrdersAsync(command.CustomerId, command.CompanyId, cancellationToken);

        var temporaryCart = BuildTemporaryCart(command);
        var priceUpdates = await _cartPricingService.ResolveCartPricingAsync(temporaryCart, cancellationToken);
        var updates = priceUpdates.ToDictionary(x => x.CartLineId);
        var orderId = Guid.NewGuid();

        var orderLines = temporaryCart.Lines.Select(line =>
        {
            var update = updates[line.Id];
            return new OrderLine(Guid.NewGuid(), orderId, line.VariantId, update.Sku, update.ProductName, update.VariantDescription, line.Quantity, update.UnitPrice, update.VatRate);
        }).ToList();

        var order = new Order(
            orderId,
            null,
            await GenerateOrderNumberAsync(cancellationToken),
            command.CustomerId,
            command.CompanyId,
            command.MarketId.Value,
            command.CurrencyCode!,
            command.CultureCode!,
            command.Email!,
            DateTime.UtcNow,
            orderLines,
            command.Addresses.Select(x => new OrderAddress(Guid.NewGuid(), orderId, x.Type, x.FirstName, x.LastName, x.CompanyName, x.Line1, x.Line2, x.PostalCode, x.City, x.Region, x.CountryCode, x.Email, x.Phone)).ToList(),
            requestedBy,
            "Manual order created.");

        await _orderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetails(order);
    }

    private async Task EnsureCompanyMembershipCanPlaceOrdersAsync(Guid? customerId, Guid? companyId, CancellationToken cancellationToken)
    {
        if (!companyId.HasValue)
        {
            return;
        }

        if (!customerId.HasValue)
        {
            throw new RequestValidationException(nameof(customerId), "CustomerId is required when creating a company order.");
        }

        var company = await _companyRepository.GetByIdAsync(companyId.Value, cancellationToken)
            ?? throw new RequestValidationException(nameof(companyId), "Unknown company.");

        var membership = company.Memberships.FirstOrDefault(x => x.CustomerId == customerId.Value);
        if (membership is null)
        {
            throw new RequestValidationException(nameof(customerId), "Customer is not a member of the company.");
        }

        if (!string.Equals(membership.Status, "Active", StringComparison.OrdinalIgnoreCase) || !membership.IsValidAt(DateTime.UtcNow) || !membership.CanPlaceOrders)
        {
            throw new RequestValidationException(nameof(customerId), "Customer is not allowed to place orders for the company.");
        }
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
            if (await _orderRepository.GetByOrderNumberAsync(orderNumber, cancellationToken) is null)
            {
                return orderNumber;
            }
        }

        throw new ConflictException("Could not allocate a unique order number.");
    }

    private static CartDomain.Cart BuildTemporaryCart(CreateOrderCommand command)
    {
        var cart = new CartDomain.Cart(
            Guid.NewGuid(),
            command.CustomerId,
            command.CompanyId,
            command.MarketId!.Value,
            command.CurrencyCode!,
            command.CultureCode!,
            command.Email,
            null,
            DateTime.UtcNow,
            DateTime.UtcNow);

        foreach (var line in command.Lines)
        {
            cart.AddLine(line.VariantId, line.VariantId.ToString(), string.Empty, null, line.Quantity, 0m, 0.25m, line.Comment);
        }

        foreach (var address in command.Addresses)
        {
            cart.AddAddress(address.Type, address.FirstName, address.LastName, address.CompanyName, address.Line1, address.Line2, address.PostalCode, address.City, address.Region, address.CountryCode, address.Email, address.Phone);
        }

        return cart;
    }

    private static OrderSummaryDto MapSummary(Order order)
    {
        return new OrderSummaryDto(
            order.Id,
            order.OrderNumber,
            order.Status,
            order.CustomerId,
            order.CompanyId,
            order.MarketId,
            order.CurrencyCode,
            order.Email,
            order.GrandTotal,
            order.PaymentStatus,
            order.FulfillmentStatus,
            order.PlacedAtUtc,
            order.RowVersion);
    }

    private static OrderDetailsDto MapDetails(Order order)
    {
        return new OrderDetailsDto(
            order.Id,
            order.SourceCartId,
            order.OrderNumber,
            order.Status,
            order.CustomerId,
            order.CompanyId,
            order.MarketId,
            order.CurrencyCode,
            order.CultureCode,
            order.Email,
            order.Subtotal,
            order.VatTotal,
            order.GrandTotal,
            order.PaymentStatus,
            order.FulfillmentStatus,
            order.PlacedAtUtc,
            order.Lines.Select(MapLine).ToList(),
            order.Addresses.Select(MapAddress).ToList(),
            order.StatusHistory.OrderByDescending(x => x.ChangedAtUtc).Select(MapStatusHistory).ToList(),
            order.PaymentTransactions.OrderByDescending(x => x.RequestedAtUtc).Select(MapPaymentTransaction).ToList(),
            order.RowVersion);
    }

    private static OrderLineDto MapLine(OrderLine line)
    {
        return new OrderLineDto(line.Id, line.VariantId, line.Sku, line.ProductName, line.VariantDescription, line.Quantity, line.UnitPrice, line.VatRate, line.LineTotal);
    }

    private static OrderAddressDto MapAddress(OrderAddress address)
    {
        return new OrderAddressDto(address.Id, address.Type, address.FirstName, address.LastName, address.CompanyName, address.Line1, address.Line2, address.PostalCode, address.City, address.Region, address.CountryCode, address.Email, address.Phone);
    }

    private static OrderStatusHistoryDto MapStatusHistory(OrderStatusHistory history)
    {
        return new OrderStatusHistoryDto(history.Id, history.FromStatus, history.ToStatus, history.ChangedBy, history.ChangedAtUtc, history.Comment);
    }

    private static PaymentTransactionDto MapPaymentTransaction(PaymentTransaction transaction)
    {
        return new PaymentTransactionDto(transaction.Id, transaction.Provider, transaction.ProviderReference, transaction.Type, transaction.Status, transaction.Amount, transaction.CurrencyCode, transaction.RequestedAtUtc, transaction.CompletedAtUtc);
    }
}
