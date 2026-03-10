namespace eyewearshop_data;

public static class OrderStatuses
{
    // Order statuses
    public const short Pending = 0; // Customer placed order, waiting for staff validation
    public const short Validated = 1; // Sales staff validated the order
    public const short Confirmed = 2; // Sales staff confirmed with customer
    public const short Processed = 10; // Order produced (for prescription items, after production is complete)
    public const short Produced = 3; // Transferred to operations, being processed
    public const short Shipped = 4; // Shipped to customer
    public const short Delivered = 5; // Delivered to customer
    public const short Cancelled = 6; // Order cancelled
    public const short Completed = 7; // Order completed (after delivery and no issues)

    
    public const short AwaitingPayment = 8; // Customer checkout order from cart, waiting for payment
    public const short Deleted = 9; // Order deleted after a period of time unpaid, or deleted by customer before payment

    // Return request statuses
    public const short ReturnRequested = 11; // Customer submitted return request
    public const short ReturnApproved = 12; // Staff approved return
    public const short ReturnRejected = 13; // Staff rejected return
}