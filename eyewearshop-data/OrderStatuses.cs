namespace eyewearshop_data;

public static class OrderStatuses
{
    // Order statuses
    public const short Pending = 0; // Customer placed order, waiting for staff validation
    public const short Validated = 1; // Sales staff validated the order
    public const short Confirmed = 2; // Sales staff confirmed with customer
    public const short Processing = 3; // Transferred to operations, being processed
    public const short Shipped = 4; // Shipped to customer
    public const short Delivered = 5; // Delivered to customer
    public const short Cancelled = 6; // Order cancelled
    public const short Completed = 7; // Order completed (after delivery and no issues)

    // Return request statuses
    public const short ReturnRequested = 10; // Customer submitted return request
    public const short ReturnApproved = 11; // Staff approved return
    public const short ReturnRejected = 12; // Staff rejected return
    public const short ReturnProcessing = 13; // Return being processed
    public const short ReturnCompleted = 14; // Return completed (refunded/exchanged)
}