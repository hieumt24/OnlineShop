using Microsoft.AspNetCore.Mvc;
using OnlineShop.Models.Db;

namespace OnlineShop.Controllers;

public class ProductsController : Controller
{
    private readonly OnlineShopContext _context;
    public ProductsController(OnlineShopContext context)
    {
        _context = context;
    }
    // GET
    public IActionResult Index()
    {
        List<Product> products = _context.Products.OrderByDescending(x => x.Id).ToList();
        return View(products);
    }
}