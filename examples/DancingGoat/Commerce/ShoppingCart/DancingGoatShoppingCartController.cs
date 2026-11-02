using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Commerce;
using CMS.ContentEngine;

using DancingGoat;
using DancingGoat.Commerce;
using DancingGoat.Helpers;
using DancingGoat.Models;
using DancingGoat.Services;
using DancingGoat.ViewComponents;

using Kentico.Commerce.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

[assembly: RegisterWebPageRoute(ShoppingCart.CONTENT_TYPE_NAME, typeof(DancingGoatShoppingCartController), WebsiteChannelNames = new[] { DancingGoatConstants.WEBSITE_CHANNEL_NAME })]

namespace DancingGoat.Commerce;

/// <summary>
/// Controller for managing the shopping cart.
/// </summary>
public sealed class DancingGoatShoppingCartController : Controller
{
    private readonly ICurrentShoppingCartRetriever currentShoppingCartRetriever;
    private readonly ICurrentShoppingCartCreator currentShoppingCartCreator;
    private readonly ProductVariantsExtractor productVariantsExtractor;
    private readonly WebPageUrlProvider webPageUrlProvider;
    private readonly ProductRepository productRepository;
    private readonly PromotionCouponRepository promotionCouponRepository;
    private readonly PromotionRepository promotionRepository;
    private readonly PromotionBreakdownProvider promotionBreakdownProvider;
    private readonly PriceCalculationTotalsCalculator priceCalculationTotalsCalculator;
    private readonly CalculationService calculationService;
    private readonly UpsellOrderDiscountService upsellOrderDiscountService;
    private readonly UpsellFreeShippingService upsellFreeShippingService;
    private readonly IStringLocalizer<SharedResources> localizer;
    private readonly IPriceFormatter priceFormatter;

    public const string UPDATE_ITEM_QUANTITY = "UpdateItemQuantity";
    public const string REMOVE_ITEM = "RemoveItem";

    public const string ADD_COUPON_CODE = "AddCoupon";
    public const string REMOVE_COUPON_CODE = "RemoveCoupon";

    /// <summary>
    /// Request header a background cart update sets to ask for the changed regions instead of a redirect.
    /// </summary>
    private const string CART_UPDATE_HEADER = "X-Cart-Update";

    private const string COUPON_CODE_ATTEMPT_TEMPDATA_KEY = "CouponCodeAttempt";
    private const string COUPON_CODE_ERROR_TEMPDATA_KEY = "CouponCodeError";


    public DancingGoatShoppingCartController(
        ICurrentShoppingCartRetriever currentShoppingCartRetriever,
        ICurrentShoppingCartCreator currentShoppingCartCreator,
        ProductVariantsExtractor productVariantsExtractor,
        WebPageUrlProvider webPageUrlProvider,
        ProductRepository productRepository,
        PromotionCouponRepository promotionCouponRepository,
        PromotionRepository promotionRepository,
        PromotionBreakdownProvider promotionBreakdownProvider,
        PriceCalculationTotalsCalculator priceCalculationTotalsCalculator,
        CalculationService calculationService,
        UpsellOrderDiscountService upsellOrderDiscountService,
        UpsellFreeShippingService upsellFreeShippingService,
        IStringLocalizer<SharedResources> localizer,
        IPriceFormatter priceFormatter)
    {
        this.currentShoppingCartRetriever = currentShoppingCartRetriever;
        this.currentShoppingCartCreator = currentShoppingCartCreator;
        this.productVariantsExtractor = productVariantsExtractor;
        this.webPageUrlProvider = webPageUrlProvider;
        this.productRepository = productRepository;
        this.promotionCouponRepository = promotionCouponRepository;
        this.promotionRepository = promotionRepository;
        this.promotionBreakdownProvider = promotionBreakdownProvider;
        this.priceCalculationTotalsCalculator = priceCalculationTotalsCalculator;
        this.calculationService = calculationService;
        this.upsellOrderDiscountService = upsellOrderDiscountService;
        this.upsellFreeShippingService = upsellFreeShippingService;
        this.localizer = localizer;
        this.priceFormatter = priceFormatter;
    }


    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await BuildShoppingCartViewModel(cancellationToken,
            TempData[COUPON_CODE_ATTEMPT_TEMPDATA_KEY] as string,
            TempData[COUPON_CODE_ERROR_TEMPDATA_KEY] as string));
    }


    /// <summary>
    /// Builds the view model behind the cart page and behind the partial that background updates swap in.
    /// </summary>
    private async Task<ShoppingCartViewModel> BuildShoppingCartViewModel(CancellationToken cancellationToken, string couponCodeAttempt = null, string couponCodeError = null)
    {
        var shoppingCart = await currentShoppingCartRetriever.Get(cancellationToken);
        if (shoppingCart == null)
        {
            return ShoppingCartViewModel.Empty;
        }

        var shoppingCartData = shoppingCart.GetShoppingCartDataModel();

        var products = await productRepository.GetProductsByIds(shoppingCartData.Items.Select(item => item.ProductIdentifier.Identifier), cancellationToken);
        var productPageUrls = await productRepository.GetProductPageUrls(products.Cast<IContentItemFieldsSource>().Select(p => p.SystemFields.ContentItemID), cancellationToken);

        var calculationResult = await calculationService.CalculateShoppingCart(shoppingCartData, cancellationToken);
        var totalWithoutShippingAndTax = priceCalculationTotalsCalculator.GetTotalWithoutShippingAndTax(calculationResult);
        var subtotalAfterLineDiscount = priceCalculationTotalsCalculator.GetSubtotalAfterLineDiscount(calculationResult);
        var linesSubtotal = priceCalculationTotalsCalculator.GetLinesSubtotal(calculationResult);
        var totalDiscount = priceCalculationTotalsCalculator.GetTotalDiscountAmount(calculationResult);
        var orderDiscount = priceCalculationTotalsCalculator.GetOrderDiscountAmount(calculationResult);

        var appliedOrderPromotionId = calculationResult.PromotionData.OrderPromotionCandidates.FirstOrDefault(c => c.Applied)?.PromotionID;
        var orderDiscountName = await promotionRepository.GetPromotionDisplayName(appliedOrderPromotionId, cancellationToken);
        var orderDiscountText = await GetOrderDiscountInfoText(calculationResult, subtotalAfterLineDiscount, shoppingCartData, cancellationToken);
        var freeShippingText = await GetFreeShippingInfoText(calculationResult, shoppingCartData, cancellationToken);
        var promotionBreakdowns = await promotionBreakdownProvider.GetPromotionBreakdowns(calculationResult, cancellationToken);

        var items = shoppingCartData.Items.Select(item =>
            {
                var product = products.FirstOrDefault(product => (product as IContentItemFieldsSource)?.SystemFields.ContentItemID == item.ProductIdentifier.Identifier);
                var variantValues = product == null ? null : productVariantsExtractor.ExtractVariantsValue(product);
                var calculationItem = calculationResult.Items.FirstOrDefault(i => i.ProductIdentifier.Identifier == item.ProductIdentifier.Identifier && i.ProductIdentifier.VariantIdentifier == item.ProductIdentifier.VariantIdentifier);

                productPageUrls.TryGetValue(item.ProductIdentifier.Identifier, out var pageUrl);

                return ((product == null) || (calculationItem == null))
                    ? null
                    : new ShoppingCartItemViewModel(
                        item.ProductIdentifier.Identifier,
                        FormatProductName(product.ProductFieldName, variantValues, item.ProductIdentifier.VariantIdentifier),
                        product.ProductFieldImage.FirstOrDefault()?.ImageFile.Url,
                        pageUrl,
                        item.Quantity,
                        calculationItem.LineSubtotalAfterLineDiscount,
                        product.ProductFieldPrice * item.Quantity,
                        item.ProductIdentifier.VariantIdentifier,
                        calculationItem.ProductData.SKU)
                    {
                        PromotionBreakdowns = promotionBreakdowns.GetValueOrDefault(calculationItem.ProductIdentifier, [])
                    };
            })
            .Where(x => x != null)
            .ToList();

        return new ShoppingCartViewModel(
            items,
            calculationResult.GrandTotal,
            linesSubtotal,
            totalWithoutShippingAndTax,
            calculationResult.TotalTax,
            totalDiscount,
            orderDiscountText,
            GetCouponsViewModel(shoppingCartData.CouponCodes, calculationResult),
            orderDiscount,
            orderDiscountName,
            couponCodeAttempt,
            couponCodeError,
            freeShippingText);
    }


    [HttpPost]
    [Route("/ShoppingCart/HandleUpdateRemove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HandleUpdateRemove(int contentItemId, int quantity, int? variantId, string action, string languageName, CancellationToken cancellationToken)
    {
        if (string.Equals(action, REMOVE_ITEM, StringComparison.OrdinalIgnoreCase))
        {
            quantity = 0;
        }

        var shoppingCart = await GetCurrentShoppingCart(cancellationToken);

        UpdateQuantity(shoppingCart, new ProductVariantIdentifier { Identifier = contentItemId, VariantIdentifier = variantId }, quantity);

        shoppingCart.Update();

        return await CartResponse(languageName, cancellationToken);
    }


    [HttpPost]
    [Route("/ShoppingCart/Add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int contentItemId, int quantity, int? variantId, string languageName, string returnUrl, CancellationToken cancellationToken)
    {
        var shoppingCart = await GetCurrentShoppingCart(cancellationToken);

        AddQuantity(shoppingCart, new ProductVariantIdentifier { Identifier = contentItemId, VariantIdentifier = variantId }, quantity);

        shoppingCart.Update();

        // This route carries no language context, so anything localized here would come out in the
        // default language. The page owns the toast text and only the badge is returned; the flag
        // below tells a full page load to show the toast once.
        if (Request.Headers.ContainsKey(CART_UPDATE_HEADER))
        {
            return ViewComponent(typeof(CartBadgeViewComponent));
        }

        TempData[DancingGoatConstants.CART_TOAST_TEMPDATA_KEY] = true;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return await RedirectToShoppingCartPage(languageName, cancellationToken);
    }


    [HttpPost]
    [Route("/ShoppingCart/HandleCouponCode")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HandleCouponCode(string couponCode, string action, string languageName, CancellationToken cancellationToken)
    {
        string couponCodeAttempt = null;
        string couponCodeError = null;

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var shoppingCart = await GetCurrentShoppingCart(cancellationToken);
            var shoppingCartData = shoppingCart.GetShoppingCartDataModel();

            // Normalize the promotion code (trim for comparison)
            var normalizedCode = couponCode.Trim();

            // A rejected code leaves the cart untouched, so it must not cause a write.
            var couponCodesChanged = false;

            if (string.Equals(action, ADD_COUPON_CODE, StringComparison.OrdinalIgnoreCase))
            {
                bool codeAlreadyApplied = shoppingCartData.CouponCodes.Any(c => string.Equals(c, normalizedCode, StringComparison.OrdinalIgnoreCase));
                bool codeExists = await promotionCouponRepository.PromotionCouponExists(normalizedCode, cancellationToken);

                if (!codeExists)
                {
                    couponCodeAttempt = normalizedCode;
                    couponCodeError = localizer["There is no promotion with the code {0}.", normalizedCode].Value;
                }
                else if (codeAlreadyApplied)
                {
                    couponCodeAttempt = normalizedCode;
                    couponCodeError = localizer["The code {0} is already applied.", normalizedCode].Value;
                }
                else
                {
                    // Add the new coupon code if it's not already present
                    shoppingCartData.CouponCodes.Add(normalizedCode);
                    couponCodesChanged = true;
                }
            }
            else if (string.Equals(action, REMOVE_COUPON_CODE, StringComparison.OrdinalIgnoreCase))
            {
                // Remove the coupon code if it's present
                var codeToRemove = shoppingCartData.CouponCodes.FirstOrDefault(c => string.Equals(c, normalizedCode, StringComparison.OrdinalIgnoreCase));
                if (codeToRemove != null)
                {
                    shoppingCartData.CouponCodes.Remove(codeToRemove);
                    couponCodesChanged = true;
                }
            }

            if (couponCodesChanged)
            {
                shoppingCart.StoreShoppingCartDataModel(shoppingCartData);
                shoppingCart.Update();
            }
        }

        return await CartResponse(languageName, cancellationToken, couponCodeAttempt, couponCodeError);
    }


    /// <summary>
    /// Answers a cart mutation. A background update asks for the changed regions and gets them as a partial;
    /// a plain form post gets the usual redirect, so the page keeps working without JavaScript.
    /// </summary>
    private async Task<IActionResult> CartResponse(string languageName, CancellationToken cancellationToken, string couponCodeAttempt = null, string couponCodeError = null)
    {
        if (!Request.Headers.ContainsKey(CART_UPDATE_HEADER))
        {
            if (couponCodeError != null)
            {
                TempData[COUPON_CODE_ATTEMPT_TEMPDATA_KEY] = couponCodeAttempt;
                TempData[COUPON_CODE_ERROR_TEMPDATA_KEY] = couponCodeError;
            }

            return await RedirectToShoppingCartPage(languageName, cancellationToken);
        }

        return PartialView("_ShoppingCartRegions", await BuildShoppingCartViewModel(cancellationToken, couponCodeAttempt, couponCodeError));
    }


    private static string FormatProductName(string productName, IDictionary<int, string> variants, int? variantId)
    {
        return variants != null && variantId != null && variants.TryGetValue(variantId.Value, out string variantValue)
            ? $"{productName} - {variantValue}"
            : productName;
    }


    /// <summary>
    /// Adds the given quantity of the product to the shopping cart, increasing the quantity of an already present item.
    /// </summary>
    private static void AddQuantity(ShoppingCartInfo shoppingCart, ProductVariantIdentifier productIdentifier, int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        var shoppingCartData = shoppingCart.GetShoppingCartDataModel();

        var productItem = shoppingCartData.Items.FirstOrDefault(x => x.ProductIdentifier == productIdentifier);
        if (productItem != null)
        {
            productItem.Quantity += quantity;
        }
        else
        {
            shoppingCartData.Items.Add(new ShoppingCartDataItem
            {
                ProductIdentifier = productIdentifier,
                Quantity = quantity
            });
        }

        shoppingCart.StoreShoppingCartDataModel(shoppingCartData);
    }


    /// <summary>
    /// Updates the quantity of the product in the shopping cart.
    /// </summary>
    private static void UpdateQuantity(ShoppingCartInfo shoppingCart, ProductVariantIdentifier productIdentifier, int quantity)
    {
        var shoppingCartData = shoppingCart.GetShoppingCartDataModel();

        var productItem = shoppingCartData.Items.FirstOrDefault(x => x.ProductIdentifier == productIdentifier);
        if (productItem != null)
        {
            productItem.Quantity = quantity;
            if (productItem.Quantity == 0)
            {
                shoppingCartData.Items.Remove(productItem);
            }
        }
        else if (quantity > 0)
        {
            shoppingCartData.Items.Add(new ShoppingCartDataItem
            {
                ProductIdentifier = productIdentifier,
                Quantity = quantity
            });
        }

        shoppingCart.StoreShoppingCartDataModel(shoppingCartData);
    }


    /// <summary>
    /// Gets the current shopping cart or creates a new one if it does not exist.
    /// </summary>
    private async Task<ShoppingCartInfo> GetCurrentShoppingCart(CancellationToken cancellationToken)
    {
        var shoppingCart = await currentShoppingCartRetriever.Get(cancellationToken);

        shoppingCart ??= await currentShoppingCartCreator.Create(cancellationToken);

        return shoppingCart;
    }


    private async Task<RedirectResult> RedirectToShoppingCartPage(string languageName, CancellationToken cancellationToken)
    {
        return Redirect(await webPageUrlProvider.ShoppingCartPageUrl(languageName, cancellationToken));
    }


    private IEnumerable<CouponCodeViewModel> GetCouponsViewModel(IEnumerable<string> couponCodes, DancingGoatPriceCalculationResult calculationResult)
    {
        return couponCodes.Select(code => new CouponCodeViewModel
        {
            Code = code,
            Status = GetCouponStatus(code, calculationResult)
        });


        // Returns the status of the coupon code in the shopping cart context.
        CouponCodeStatus GetCouponStatus(string couponCode, DancingGoatPriceCalculationResult calculationResult)
        {
            foreach (var item in calculationResult.Items)
            {
                var catalogCandidate = item.PromotionData.CatalogPromotionCandidates.FirstOrDefault(
                    c => c.CouponCode?.Equals(couponCode, StringComparison.OrdinalIgnoreCase) ?? false);

                if (catalogCandidate != null)
                {
                    return catalogCandidate.Applied ? CouponCodeStatus.Applied : CouponCodeStatus.Applicable;
                }

                var cartItemCandidate = item.PromotionData.CartItemPromotionCandidates.FirstOrDefault(
                    c => c.CouponCode?.Equals(couponCode, StringComparison.OrdinalIgnoreCase) ?? false);

                if (cartItemCandidate != null)
                {
                    return cartItemCandidate.Applied ? CouponCodeStatus.Applied : CouponCodeStatus.Applicable;
                }
            }

            var orderCandidate = calculationResult.PromotionData.OrderPromotionCandidates.FirstOrDefault(
                c => c.CouponCode?.Equals(couponCode, StringComparison.OrdinalIgnoreCase) ?? false);

            if (orderCandidate != null)
            {
                return orderCandidate.Applied ? CouponCodeStatus.Applied : CouponCodeStatus.Applicable;
            }

            var shippingCandidate = calculationResult.PromotionData.FreeShippingPromotionCandidates.FirstOrDefault(
                c => c.CouponCode?.Equals(couponCode, StringComparison.OrdinalIgnoreCase) ?? false);

            if (shippingCandidate != null)
            {
                return shippingCandidate.Applied ? CouponCodeStatus.Applied : CouponCodeStatus.Applicable;
            }

            return CouponCodeStatus.NotApplicable;
        }
    }


    private async Task<string> GetOrderDiscountInfoText(DancingGoatPriceCalculationResult calculationResult, decimal subtotalAfterLineDiscount, ShoppingCartDataModel shoppingCartData, CancellationToken cancellationToken)
    {
        var appliedOrderPromotion = calculationResult.PromotionData.OrderPromotionCandidates.FirstOrDefault(c => c.Applied);
        var appliedOrderDiscountAmount = appliedOrderPromotion?.PromotionCandidate.OrderDiscountAmount ?? 0;

        var text = await upsellOrderDiscountService.GetUpsellOrderDiscountMessage(subtotalAfterLineDiscount, appliedOrderDiscountAmount, appliedOrderPromotion?.PromotionID, shoppingCartData.CouponCodes, cancellationToken);

        if (string.IsNullOrEmpty(text))
        {
            if (appliedOrderPromotion != null)
            {
                var orderDiscountMessageSource = localizer["You qualified for a {0} discount. Enjoy your discount!"];
                var priceString = priceFormatter.Format(appliedOrderPromotion.PromotionCandidate.OrderDiscountAmount, new PriceFormatContext());
                text = string.Format(orderDiscountMessageSource, priceString);
            }
        }

        return text;
    }


    /// <summary>
    /// Returns the free shipping confirmation when the cart qualifies, otherwise the upsell message toward the
    /// closest active free shipping promotion. Returns <c>null</c> when there is nothing to tell the customer.
    /// </summary>
    private async Task<string> GetFreeShippingInfoText(DancingGoatPriceCalculationResult calculationResult, ShoppingCartDataModel shoppingCartData, CancellationToken cancellationToken)
    {
        if (calculationResult.PromotionData.FreeShippingPromotionCandidates.Any(candidate => candidate.Applied))
        {
            return localizer["You qualified for free shipping. Enjoy!"];
        }

        return await upsellFreeShippingService.GetUpsellFreeShippingMessage(calculationResult, shoppingCartData.CouponCodes, cancellationToken);
    }
}
