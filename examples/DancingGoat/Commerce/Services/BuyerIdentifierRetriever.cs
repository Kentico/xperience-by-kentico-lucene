using System.Threading.Tasks;

using CMS.Commerce;

using Kentico.Membership;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DancingGoat.Commerce;

/// <summary>
/// Retrieves the buyer identifier of the current visitor.
/// </summary>
public sealed class BuyerIdentifierRetriever
{
    private readonly IHttpContextAccessor httpContextAccessor;


    /// <summary>
    /// Creates a new instance of <see cref="BuyerIdentifierRetriever"/>.
    /// </summary>
    public BuyerIdentifierRetriever(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }


    /// <summary>
    /// Gets the buyer identifier of the signed in member, or of an anonymous visitor when nobody is signed in.
    /// </summary>
    public async Task<BuyerIdentifier> GetCurrentBuyerIdentifier()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return BuyerIdentifier.FromMemberId(0);
        }

        var userManager = httpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.GetUserAsync(httpContext.User);

        return BuyerIdentifier.FromMemberId(user?.Id ?? 0);
    }
}
