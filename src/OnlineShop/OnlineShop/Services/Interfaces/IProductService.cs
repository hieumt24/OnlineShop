using OnlineShop.Models.Db;

namespace OnlineShop.Services.Interfaces;

public interface IProductService
{
    List<Product> GetBestSellingProducts(int top = 10);
}