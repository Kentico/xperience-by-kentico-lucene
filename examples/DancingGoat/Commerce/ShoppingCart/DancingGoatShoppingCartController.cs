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

using Kentico.Commerce.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;

using Microsoft.AspNetCore.Mvc;

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
    private readonly CalculationService calculationService;

    public DancingGoatShoppingCartController(
        ICurrentShoppingCartRetriever currentShoppingCartRetriever,
        ICurrentShoppingCartCreator currentShoppingCartCreator,
        ProductVariantsExtractor productVariantsExtractor,
        WebPageUrlProvider webPageUrlProvider,
        ProductRepository productRepository,
        CalculationService calculationService)
    {
        this.currentShoppingCartRetriever = currentShoppingCartRetriever;
        this.currentShoppingCartCreator = currentShoppingCartCreator;
        this.productVariantsExtractor = productVariantsExtractor;
        this.webPageUrlProvider = webPageUrlProvider;
        this.productRepository = productRepository;
        this.calculationService = calculationService;
    }


    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var shoppingCart = await currentShoppingCartRetriever.Get(cancellationToken);
        if (shoppingCart == null)
        {
            return View(new ShoppingCartViewModel(new List<ShoppingCartItemViewModel>(), 0, 0, 0, 0));
        }

        var shoppingCartData = shoppingCart.GetShoppingCartDataModel();

        var products = await productRepository.GetProductsByIds(shoppingCartData.Items.Select(item => item.ProductIdentifier.Identifier), cancellationToken);

        var productPageUrls = await productRepository.GetProductPageUrls(products.Cast<IContentItemFieldsSource>().Select(p => p.SystemFields.ContentItemID), cancellationToken);

        var calculationResult = await calculationService.CalculateShoppingCart(shoppingCartData, cancellationToken);

        var totalWithoutTax = calculationResult.Items.Sum(x => x.LineSubtotalAfterAllDiscounts);
        var subtotal = calculationResult.Items.Sum(x => x.LineSubtotalAfterLineDiscount);

        var orderDiscount = calculationResult.PromotionData.OrderPromotionCandidates.FirstOrDefault()?.PromotionCandidate.OrderDiscountAmount ?? 0;

        return View(new ShoppingCartViewModel(
            shoppingCartData.Items.Select(item =>
            {
                var product = products.FirstOrDefault(product => (product as IContentItemFieldsSource)?.SystemFields.ContentItemID == item.ProductIdentifier.Identifier);
                var variantValues = product == null ? null : productVariantsExtractor.ExtractVariantsValue(product);
                var calculationItem = calculationResult.Items.FirstOrDefault(i => i.ProductIdentifier.Identifier == item.ProductIdentifier.Identifier && i.ProductIdentifier.VariantIdentifier == item.ProductIdentifier.VariantIdentifier);

                productPageUrls.TryGetValue(item.ProductIdentifier.Identifier, out var pageUrl);

                return product == null
                    ? null
                    : new ShoppingCartItemViewModel(
                        item.ProductIdentifier.Identifier,
                        FormatProductName(product.ProductFieldName, variantValues, item.ProductIdentifier.VariantIdentifier),
                        product.ProductFieldImage.FirstOrDefault()?.ImageFile.Url,
                        pageUrl,
                        item.Quantity,
                        calculationItem.LineSubtotalAfterLineDiscount / calculationItem.Quantity,
                        calculationItem?.LineSubtotalAfterLineDiscount ?? product.ProductFieldPrice,
                        product.ProductFieldPrice * item.Quantity,
                        calculationItem.PromotionData.CatalogPromotionCandidates.FirstOrDefault(c => c.Applied)?.PromotionCandidate as DancingGoatCatalogPromotionCandidate,
                        item.ProductIdentifier.VariantIdentifier);
            })
            .Where(x => x != null)
            .ToList(),
            totalWithoutTax,
            subtotal,
            calculationResult.TotalTax,
            orderDiscount));
    }


    [HttpPost]
    [Route("/ShoppingCart/HandleAddRemove")]
    public async Task<IActionResult> HandleAddRemove(int contentItemId, int quantity, int? variantId, string action, string languageName)
    {
        if (string.Equals(action, "Remove", StringComparison.OrdinalIgnoreCase))
        {
            quantity *= -1;
        }
        else if (action == "RemoveAll")
        {
            quantity = 0;
        }

        var shoppingCart = await GetCurrentShoppingCart();

        UpdateQuantity(shoppingCart, new ProductVariantIdentifier { Identifier = contentItemId, VariantIdentifier = variantId }, quantity, setAbsoluteValue: new[] { "RemoveAll", "Update" }.Contains(action));

        shoppingCart.Update();

        return Redirect(await webPageUrlProvider.ShoppingCartPageUrl(languageName));
    }


    [HttpPost]
    [Route("/ShoppingCart/Add")]
    public async Task<IActionResult> Add(int contentItemId, int quantity, int? variantId, string languageName)
    {
        var shoppingCart = await GetCurrentShoppingCart();

        UpdateQuantity(shoppingCart, new ProductVariantIdentifier { Identifier = contentItemId, VariantIdentifier = variantId }, quantity);

        shoppingCart.Update();

        return Redirect(await webPageUrlProvider.ShoppingCartPageUrl(languageName));
    }


    private static string FormatProductName(string productName, IDictionary<int, string> variants, int? variantId)
    {
        return variants != null && variantId != null && variants.TryGetValue(variantId.Value, out string variantValue)
            ? $"{productName} - {variantValue}"
            : productName;
    }


    /// <summary>
    /// Updates the quantity of the product in the shopping cart.
    /// </summary>
    private static void UpdateQuantity(ShoppingCartInfo shoppingCart, ProductVariantIdentifier productIdentifier, int quantity, bool setAbsoluteValue = false)
    {
        var shoppingCartData = shoppingCart.GetShoppingCartDataModel();

        var productItem = shoppingCartData.Items.FirstOrDefault(x => x.ProductIdentifier == productIdentifier);
        if (productItem != null)
        {
            productItem.Quantity = setAbsoluteValue ? quantity : Math.Max(0, productItem.Quantity + quantity);
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
    private async Task<ShoppingCartInfo> GetCurrentShoppingCart()
    {
        var shoppingCart = await currentShoppingCartRetriever.Get();

        shoppingCart ??= await currentShoppingCartCreator.Create();

        return shoppingCart;
    }
}
