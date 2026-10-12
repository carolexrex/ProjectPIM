using System.Text.Json;
using Platform.Application.Abstractions.Errors;
using Platform.Application.Abstractions.Persistence;
using Platform.Application.Cart;
using Platform.Application.Orders;
using Platform.Application.Orders.Commands;
using Platform.Application.Storefront;
using Platform.Contracts.Orders;
using Platform.Contracts.Storefront;
using Platform.Domain.Common;
using Platform.Infrastructure.Cart;
using CartDomain = Platform.Domain.Cart;

namespace Platform.Infrastructure.Storefront;

public sealed class StorefrontCartApplicationService : IStorefrontCartApplicationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> StockReasons = new(["OutOfStock", "Unavailable"], StringComparer.Ordinal);

    private readonly ICartRepository _cartRepository;
    private readonly IStorefrontContextApplicationService _contextService;
    private readonly IStorefrontProductProjectionRepository _projectionRepository;
    private readonly CartAdminApplicationService _cartPricingService;
    private readonly IOrderAdminApplicationService _orderService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorefrontCartAccessTokenService _cartAccessTokenService;

    public StorefrontCartApplicationService(
        ICartRepository cartRepository,
        IStorefrontContextApplicationService contextService,
        IStorefrontProductProjectionRepository projectionRepository,
        CartAdminApplicationService cartPricingService,
        IOrderAdminApplicationService orderService,
        IUnitOfWork unitOfWork,
        IStorefrontCartAccessTokenService cartAccessTokenService)
    {
        _cartRepository = cartRepository;
        _contextService = contextService;
        _projectionRepository = projectionRepository;
        _cartPricingService = cartPricingService;
        _orderService = orderService;
        _unitOfWork = unitOfWork;
        _cartAccessTokenService = cartAccessTokenService;
    }

    public async Task<StorefrontCartResult> CreateAsync(CreateStorefrontCartCommand command, CancellationToken cancellationToken)
    {
        if (command.Lines.Count == 0)
        {
            return StorefrontCartResult.Invalid(nameof(command.Lines), "A storefront cart must contain at least one line.");
        }

        var contextResult = await _contextService.GetContextAsync(
            new GetStorefrontContextQuery(
                command.ChannelCode,
                command.MarketCode,
                command.CultureCode,
                command.CurrencyCode,
                command.HostName),
            cancellationToken);

        if (contextResult.Status != StorefrontContextResolutionStatus.Success || contextResult.Context is null)
        {
            return StorefrontCartResult.FromContextFailure(contextResult);
        }

        try
        {
            var now = DateTime.UtcNow;
            var cart = new CartDomain.Cart(
                Guid.NewGuid(),
                customerId: null,
                companyId: null,
                contextResult.Context.Market.Id,
                contextResult.Context.ActiveCurrencyCode,
                contextResult.Context.ActiveCultureCode,
                command.Email,
                now.AddDays(30),
                now,
                now);

            foreach (var line in command.Lines)
            {
                cart.AddLine(
                    line.VariantId,
                    line.VariantId.ToString(),
                    string.Empty,
                    null,
                    line.Quantity,
                    unitPrice: 0m,
                    vatRate: 0m,
                    line.Comment);
            }

            foreach (var address in command.Addresses)
            {
                cart.AddAddress(
                    address.Type,
                    address.FirstName,
                    address.LastName,
                    address.CompanyName,
                    address.Line1,
                    address.Line2,
                    address.PostalCode,
                    address.City,
                    address.Region,
                    address.CountryCode,
                    address.Email,
                    address.Phone);
            }

            var buyabilityErrors = await ValidateLinesAreBuyableAsync(
                cart.Lines,
                contextResult.Context.Market.Code,
                contextResult.Context.ActiveCultureCode,
                contextResult.Context.ActiveCurrencyCode,
                cancellationToken);
            if (buyabilityErrors.Count > 0)
            {
                return StorefrontCartResult.Invalid(buyabilityErrors);
            }

            var updates = await _cartPricingService.ResolveCartPricingAsync(cart, cancellationToken);
            cart.Reprice(updates, cart.RowVersion);

            await _cartRepository.AddAsync(cart, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return StorefrontCartResult.Success(MapCart(cart));
        }
        catch (RequestValidationException exception)
        {
            return StorefrontCartResult.Invalid(exception.Errors);
        }
        catch (InvalidOperationException exception)
        {
            return StorefrontCartResult.Invalid(nameof(command.Lines), exception.Message);
        }
    }

    public async Task<StorefrontCartResult> GetByIdAsync(GetStorefrontCartByIdQuery query, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(query.CartId, cancellationToken);
        if (cart is null)
        {
            return StorefrontCartResult.NotFound("Cart", query.CartId.ToString());
        }

        if (!_cartAccessTokenService.IsValid(cart, query.CartAccessToken))
        {
            return StorefrontCartResult.Unauthorized();
        }

        return StorefrontCartResult.Success(MapCart(cart));
    }

    public async Task<StorefrontCartResult> RepriceAsync(RepriceStorefrontCartCommand command, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(command.CartId, cancellationToken);
        if (cart is null)
        {
            return StorefrontCartResult.NotFound("Cart", command.CartId.ToString());
        }

        if (!_cartAccessTokenService.IsValid(cart, command.CartAccessToken))
        {
            return StorefrontCartResult.Unauthorized();
        }

        try
        {
            var buyabilityErrors = await ValidateLinesAreBuyableAsync(
                cart.Lines,
                cart.MarketId,
                cart.CultureCode,
                cart.CurrencyCode,
                cancellationToken);
            if (buyabilityErrors.Count > 0)
            {
                return StorefrontCartResult.Invalid(buyabilityErrors);
            }

            var updates = await _cartPricingService.ResolveCartPricingAsync(cart, cancellationToken);
            cart.Reprice(updates, command.RowVersion);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return StorefrontCartResult.Success(MapCart(cart));
        }
        catch (RequestValidationException exception)
        {
            return StorefrontCartResult.Invalid(exception.Errors);
        }
        catch (ConcurrencyException exception)
        {
            return StorefrontCartResult.Invalid(nameof(command.RowVersion), exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return StorefrontCartResult.Invalid(nameof(command.CartId), exception.Message);
        }
    }

    public async Task<StorefrontCheckoutResult> CheckoutAsync(CheckoutStorefrontCartCommand command, CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByIdAsync(command.CartId, cancellationToken);
        if (cart is null)
        {
            return StorefrontCheckoutResult.NotFound("Cart", command.CartId.ToString());
        }

        if (!_cartAccessTokenService.IsValid(cart, command.CartAccessToken))
        {
            return StorefrontCheckoutResult.Unauthorized();
        }

        // A converted cart is a repeated checkout: skip the pre-checks (its stock is now reserved, so
        // the projection may already say out of stock) and let order creation return the original order.
        var isRepeat = !string.Equals(cart.Status, "Active", StringComparison.OrdinalIgnoreCase);
        if (!isRepeat && string.IsNullOrWhiteSpace(cart.Email))
        {
            return StorefrontCheckoutResult.Invalid(nameof(cart.Email), "Cart email is required before checkout.");
        }

        var addressErrors = isRepeat ? [] : ValidateCheckoutAddresses(cart);
        if (addressErrors.Count > 0)
        {
            return StorefrontCheckoutResult.Invalid(addressErrors);
        }

        try
        {
            // Projected stock can lag; order placement reserves against live balances and is the
            // stock authority at checkout, so only visibility, status, and price are checked here.
            var buyabilityErrors = isRepeat
                ? []
                : await ValidateLinesAreBuyableAsync(
                    cart.Lines,
                    cart.MarketId,
                    cart.CultureCode,
                    cart.CurrencyCode,
                    cancellationToken);
            if (buyabilityErrors.Count > 0)
            {
                return StorefrontCheckoutResult.Invalid(buyabilityErrors);
            }

            var order = await _orderService.CreateAsync(
                new CreateOrderCommand(
                    CartId: cart.Id,
                    CartRowVersion: command.RowVersion,
                    CustomerId: null,
                    CompanyId: null,
                    MarketId: null,
                    CurrencyCode: null,
                    CultureCode: null,
                    Email: null,
                    Lines: [],
                    Addresses: []),
                "storefront",
                cancellationToken);

            return StorefrontCheckoutResult.Success(MapOrder(order));
        }
        catch (RequestValidationException exception)
        {
            return StorefrontCheckoutResult.Invalid(exception.Errors);
        }
        catch (ConcurrencyException exception)
        {
            return StorefrontCheckoutResult.Invalid(nameof(command.RowVersion), exception.Message);
        }
        catch (ConcurrentWriteException exception)
        {
            return StorefrontCheckoutResult.Conflict(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return StorefrontCheckoutResult.Invalid(nameof(command.CartId), exception.Message);
        }
    }

    private async Task<Dictionary<string, string[]>> ValidateLinesAreBuyableAsync(
        IEnumerable<CartDomain.CartLine> lines,
        string marketCode,
        string cultureCode,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        var projections = await _projectionRepository.ListByContextAsync(marketCode, cultureCode, currencyCode, cancellationToken);
        return ValidateLinesAreBuyable(lines, projections);
    }

    private async Task<Dictionary<string, string[]>> ValidateLinesAreBuyableAsync(
        IEnumerable<CartDomain.CartLine> lines,
        Guid marketId,
        string cultureCode,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        var projections = await _projectionRepository.ListByContextAsync(marketId, cultureCode, currencyCode, cancellationToken);
        return ValidateLinesAreBuyable(lines, projections, useProjectedStock: false);
    }

    /// <summary>
    /// Validates lines against projected buyability. With <paramref name="useProjectedStock"/> false
    /// (checkout), stock reasons and projected quantities are ignored because reservation checks live
    /// balances; cart creation keeps the projected stock check as early feedback.
    /// </summary>
    private static Dictionary<string, string[]> ValidateLinesAreBuyable(
        IEnumerable<CartDomain.CartLine> lines,
        IReadOnlyList<StorefrontProductProjection> projections,
        bool useProjectedStock = true)
    {
        var errors = new Dictionary<string, string[]>();
        var variantsById = projections
            .Where(x => x.IsVisible)
            .SelectMany(ParseVariants)
            .GroupBy(x => x.Id)
            .ToDictionary(x => x.Key, x => x.First());

        foreach (var line in lines)
        {
            var key = $"{nameof(line.VariantId)}.{line.VariantId}";
            if (!variantsById.TryGetValue(line.VariantId, out var variant))
            {
                errors[key] = [$"Variant {line.VariantId} is not visible in the storefront context."];
                continue;
            }

            var blockingReasons = useProjectedStock
                ? variant.Buyability.Reasons
                : variant.Buyability.Reasons.Where(x => !StockReasons.Contains(x)).ToList();
            var isBuyable = useProjectedStock ? variant.Buyability.IsBuyable : blockingReasons.Count == 0;
            if (!isBuyable)
            {
                var reasons = blockingReasons.Count == 0
                    ? "not buyable"
                    : string.Join(", ", blockingReasons);
                errors[key] = [$"Variant {variant.Sku} is not buyable in the storefront context: {reasons}."];
                continue;
            }

            if (useProjectedStock && !variant.Availability.IsBackorderable && line.Quantity > variant.Availability.AvailableQuantity)
            {
                errors[key] = [$"Variant {variant.Sku} has only {variant.Availability.AvailableQuantity} available in the storefront context."];
            }
        }

        return errors;
    }

    private static IReadOnlyList<StorefrontProductVariantDto> ParseVariants(StorefrontProductProjection projection)
    {
        return JsonSerializer.Deserialize<List<StorefrontProductVariantDto>>(projection.VariantsJson, JsonOptions) ?? [];
    }

    private static Dictionary<string, string[]> ValidateCheckoutAddresses(CartDomain.Cart cart)
    {
        var errors = new Dictionary<string, string[]>();
        if (!cart.Addresses.Any(x => string.Equals(x.Type, "Billing", StringComparison.OrdinalIgnoreCase)))
        {
            errors[nameof(cart.Addresses)] = ["A billing address is required before checkout."];
        }

        if (!cart.Addresses.Any(x => string.Equals(x.Type, "Shipping", StringComparison.OrdinalIgnoreCase)))
        {
            errors[nameof(cart.Addresses)] = errors.TryGetValue(nameof(cart.Addresses), out var existing)
                ? existing.Concat(["A shipping address is required before checkout."]).ToArray()
                : ["A shipping address is required before checkout."];
        }

        return errors;
    }

    private StorefrontCartDto MapCart(CartDomain.Cart cart)
    {
        return new StorefrontCartDto(
            cart.Id,
            cart.MarketId,
            cart.CurrencyCode,
            cart.CultureCode,
            cart.Email,
            cart.Status,
            cart.Subtotal,
            cart.VatTotal,
            cart.GrandTotal,
            cart.ExpiresAtUtc,
            cart.Lines.Select(line => new StorefrontCartLineDto(
                line.Id,
                line.VariantId,
                line.Sku,
                line.ProductName,
                line.VariantDescription,
                line.Quantity,
                line.UnitPrice,
                line.VatRate,
                line.LineTotal,
                line.Comment)).ToList(),
            cart.Addresses.Select(address => new StorefrontCartAddressDto(
                address.Id,
                address.Type,
                address.FirstName,
                address.LastName,
                address.CompanyName,
                address.Line1,
                address.Line2,
                address.PostalCode,
                address.City,
                address.Region,
                address.CountryCode,
                address.Email,
                address.Phone)).ToList(),
            cart.CreatedAtUtc,
            cart.UpdatedAtUtc,
            cart.RowVersion,
            _cartAccessTokenService.CreateToken(cart));
    }

    private static StorefrontOrderDto MapOrder(OrderDetailsDto order)
    {
        return new StorefrontOrderDto(
            order.Id,
            order.SourceCartId ?? Guid.Empty,
            order.OrderNumber,
            order.Status,
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
            order.Lines.Select(line => new StorefrontOrderLineDto(
                line.Id,
                line.VariantId,
                line.Sku,
                line.ProductName,
                line.VariantDescription,
                line.Quantity,
                line.UnitPrice,
                line.VatRate,
                line.LineTotal)).ToList(),
            order.Addresses.Select(address => new StorefrontOrderAddressDto(
                address.Id,
                address.Type,
                address.FirstName,
                address.LastName,
                address.CompanyName,
                address.Line1,
                address.Line2,
                address.PostalCode,
                address.City,
                address.Region,
                address.CountryCode,
                address.Email,
                address.Phone)).ToList(),
            order.RowVersion);
    }
}
