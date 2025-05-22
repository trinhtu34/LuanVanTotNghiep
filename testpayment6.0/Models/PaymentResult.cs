using System;
using System.Collections.Generic;

namespace testpayment6._0.Models;

public partial class PaymentResult
{
    public long PaymentResultId { get; set; }

    public long OrderTableId { get; set; }

    public long PaymentId { get; set; }

    public bool IsSuccess { get; set; }

    public string? Description { get; set; }

    public DateTime Timestamp { get; set; }

    public long? VnpayTransactionId { get; set; }

    public string? PaymentMethod { get; set; }

    public string? BankCode { get; set; }

    public string? BankTransactionId { get; set; }

    public string? ResponseDescription { get; set; }

    public string? TransactionStatusDescription { get; set; }

    public virtual OrderTable OrderTable { get; set; } = null!;
}
