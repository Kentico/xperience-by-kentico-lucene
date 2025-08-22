using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using CMS.Commerce;
using CMS.DataEngine;

namespace DancingGoat.Commerce;

/// <summary>
/// Service responsible for generating unique order numbers.
/// </summary>
public sealed partial class OrderNumberGenerator
{
    private readonly IInfoProvider<OrderInfo> orderInfoProvider;


    public OrderNumberGenerator(IInfoProvider<OrderInfo> orderInfoProvider)
    {
        this.orderInfoProvider = orderInfoProvider;
    }


    /// <summary>
    /// Generates a new unique order number in the format "YYYY-N", where N is the sequential number for the year.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the generated order number.</returns>
    public async Task<string> GenerateOrderNumber(CancellationToken cancellationToken)
    {
        var actualYear = DateTime.Now.Year;
        var beginningOfTheYear = new DateTime(actualYear, 1, 1);

        var lastOrderNumber = await orderInfoProvider.Get()
                                                     .WhereGreaterOrEquals(nameof(OrderInfo.OrderCreatedWhen), beginningOfTheYear)
                                                     .OrderByDescending(nameof(OrderInfo.OrderCreatedWhen))
                                                     .TopN(1)
                                                     .Column(nameof(OrderInfo.OrderNumber))
                                                     .GetScalarResultAsync<string>(cancellationToken: cancellationToken);

        var orderSequenceNumber = GetNextOrderSequenceNumber(lastOrderNumber);

        return FormatOrderNumber(actualYear, orderSequenceNumber);
    }


    /// <summary>
    /// Formats the order number using the given year and sequence number.
    /// </summary>
    /// <param name="year">The year to include in the order number.</param>
    /// <param name="orderSequenceNumber">The sequential number for the given year.</param>
    /// <returns>The formatted order number string.</returns>
    private static string FormatOrderNumber(int year, int orderSequenceNumber) => $"{year}-{orderSequenceNumber}";


    /// <summary>
    /// Parses the last order number and calculates the next sequential number.
    /// </summary>
    /// <param name="orderNumber">The last generated order number in the format "YYYY-N".</param>
    /// <returns>The next sequence number for the current year.</returns>
    private static int GetNextOrderSequenceNumber(string orderNumber)
    {
        var match = LastNumberRegex().Match(orderNumber ?? string.Empty);
        var parsedSequenceNumber = match.Success && int.TryParse(match.Groups[1].Value, out var parsedNumber)
            ? parsedNumber
            : 0;

        return parsedSequenceNumber + 1;
    }


    /// <summary>
    /// Regex to extract the numeric sequence from the end of the order number (e.g., "2025-12" → 12).
    /// </summary>
    /// <returns>The compiled regex instance.</returns>
    [GeneratedRegex(@"-(\d+)$")]
    private static partial Regex LastNumberRegex();
}
