namespace Nuxie.Unity;

public sealed class PurchaseRequest
{
  public required string RequestId { get; init; }
  public required string Platform { get; init; }
  public required string ProductId { get; init; }
  public string? BasePlanId { get; init; }
  public string? OfferId { get; init; }
  public string? DisplayName { get; init; }
  public string? DisplayPrice { get; init; }
  public double? Price { get; init; }
  public string? CurrencyCode { get; init; }
  public required long TimestampMs { get; init; }
}

public sealed class RestoreRequest
{
  public required string RequestId { get; init; }
  public required string Platform { get; init; }
  public required long TimestampMs { get; init; }
}

public enum PurchaseResultType
{
  Success,
  Cancelled,
  Pending,
  Failed,
}

public sealed class PurchaseResult
{
  public required PurchaseResultType Type { get; init; }
  public string? Message { get; init; }
  public string? ProductId { get; init; }
  public string? PurchaseToken { get; init; }
  public string? OrderId { get; init; }
  public string? TransactionId { get; init; }
  public string? OriginalTransactionId { get; init; }
  public string? TransactionJws { get; init; }

  public static PurchaseResult Success(
    string? productId = null,
    string? purchaseToken = null,
    string? orderId = null,
    string? transactionId = null,
    string? originalTransactionId = null,
    string? transactionJws = null
  )
  {
    return new PurchaseResult
    {
      Type = PurchaseResultType.Success,
      ProductId = productId,
      PurchaseToken = purchaseToken,
      OrderId = orderId,
      TransactionId = transactionId,
      OriginalTransactionId = originalTransactionId,
      TransactionJws = transactionJws,
    };
  }

  public static PurchaseResult Cancelled()
  {
    return new PurchaseResult { Type = PurchaseResultType.Cancelled };
  }

  public static PurchaseResult Pending()
  {
    return new PurchaseResult { Type = PurchaseResultType.Pending };
  }

  public static PurchaseResult Failed(string message)
  {
    return new PurchaseResult { Type = PurchaseResultType.Failed, Message = message };
  }
}

public enum RestoreResultType
{
  Success,
  NoPurchases,
  Failed,
}

public sealed class RestoreResult
{
  public required RestoreResultType Type { get; init; }
  public int? RestoredCount { get; init; }
  public string? Message { get; init; }

  public static RestoreResult Success(int? restoredCount = null)
  {
    return new RestoreResult { Type = RestoreResultType.Success, RestoredCount = restoredCount };
  }

  public static RestoreResult NoPurchases()
  {
    return new RestoreResult { Type = RestoreResultType.NoPurchases };
  }

  public static RestoreResult Failed(string message)
  {
    return new RestoreResult { Type = RestoreResultType.Failed, Message = message };
  }
}

public interface INuxiePurchaseController
{
  System.Threading.Tasks.Task<PurchaseResult> OnPurchaseAsync(PurchaseRequest request);
  System.Threading.Tasks.Task<RestoreResult> OnRestoreAsync(RestoreRequest request);
}
