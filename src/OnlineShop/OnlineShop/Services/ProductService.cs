using OnlineShop.Models.Db;
using OnlineShop.Services.Interfaces;

namespace OnlineShop.Services;

public class ProductService : IProductService
{
    private readonly OnlineShopContext _context;
    public ProductService(OnlineShopContext context)
    {
        _context = context;
    }
    public List<Product> GetBestSellingProducts(int top = 10)
    {
        var bestSellingProducts = _context.OrderDetails
            .GroupBy(od => od.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                TotalSold = g.Sum(od => od.Count)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(top)
            .Join(_context.Products,
                result => result.ProductId,
                product => product.Id,
                (result, product) => product)
            .ToList();
        return bestSellingProducts;
    }
}