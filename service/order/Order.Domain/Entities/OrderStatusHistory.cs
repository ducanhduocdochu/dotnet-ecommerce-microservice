namespace Order.Domain.Entities;

public class OrderStatusHistory
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public string Status { get; private set; } = null!;
    public string? PreviousStatus { get; private set; }
    public string? Note { get; private set; }
    public Guid? ChangedBy { get; private set; }
    public string? ChangedByName { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation
    public OrderEntity? Order { get; set; }

    private OrderStatusHistory() { }

    public OrderStatusHistory(Guid orderId, string status, string? previousStatus = null, string? note = null, Guid? changedBy = null, string? changedByName = null)
    {
        OrderId = orderId;
        Status = status;
        PreviousStatus = previousStatus;
        Note = note;
        ChangedBy = changedBy;
        ChangedByName = changedByName;
    }
}

