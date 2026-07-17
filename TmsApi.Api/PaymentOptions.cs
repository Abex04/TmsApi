using System.ComponentModel.DataAnnotations;

/// <summary>
/// Strongly-typed configuration for the TMS payment gateway.
/// Validated at startup — the app refuses to start if any required value is missing.
/// </summary>
public class PaymentOptions
{
    /// <summary>
    /// The URL of the external payment gateway (required).
    /// </summary>
    [Required]
    public required string GatewayUrl { get; init; }

    /// <summary>
    /// Maximum deposit amount in Ethiopian Birr (must be between 100 and 100,000).
    /// </summary>
    [Range(100, 100_000)]
    public decimal MaxDepositBirr { get; init; }
}