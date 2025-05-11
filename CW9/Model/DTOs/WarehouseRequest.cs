namespace CW9.Model.DTOs;

public class WarehouseRequest
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}