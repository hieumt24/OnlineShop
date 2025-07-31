namespace OnlineShop.Models.Db;

public partial class OrderDetails
{
    public int Id { get; set; }
    
    public string ProductTitle { get; set; } = null!;
    
    public decimal ProductPrice { get; set; }
    
    public int Count { get; set; }
    
    public int OrderId { get; set; }
    
    public int ProductId { get; set; }
}