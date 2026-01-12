using System.Threading;
using System.Threading.Tasks;

using CMS.DataEngine;
using CMS.Helpers;

namespace DancingGoat.Commerce;

/// <summary>
/// Service responsible for generating unique order numbers.
/// </summary>
public sealed class OrderNumberGenerator
{
    private const string SEQUENCE_NAME = "OrderNumberSequence";


    /// <summary>
    /// Generates a new unique order number using the underlying SQL sequence.
    /// The format is <c>ORD#NNNNNN</c>, where <c>NNNNNN</c> is a zero-padded
    /// sequential value retrieved from the database.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the generated order number string.
    /// </returns>
    public async Task<string> GenerateOrderNumber(CancellationToken cancellationToken)
    {
        var sequence = await GetNextOrderSequenceNumber(cancellationToken);
        return $"ORD#{sequence:D6}";
    }


    /// <summary>
    /// Retrieves the next value from the configured SQL sequence.
    /// If the sequence does not exist in the database, it will be created automatically
    /// before retrieving the next value.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the next numeric value from the sequence.
    /// </returns>
    private static async Task<long> GetNextOrderSequenceNumber(CancellationToken cancellationToken)
    {
        var query = $"""
                         IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = N'{SEQUENCE_NAME}')
                         BEGIN
                             CREATE SEQUENCE [{SEQUENCE_NAME}]
                                 AS BIGINT
                                 START WITH 1
                                 INCREMENT BY 1
                                 MINVALUE 1
                                 NO CYCLE
                                 CACHE 100;
                         END;

                         SELECT NEXT VALUE FOR [{SEQUENCE_NAME}];
                     """;

        var scalar = await ConnectionHelper.ExecuteScalarAsync(query,
            null,
            QueryTypeEnum.SQLQuery,
            cancellationToken);

        return ValidationHelper.GetLong(scalar, 0);
    }
}
