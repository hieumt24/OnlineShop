using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public IActionResult SearchProduct(string searchText)
    {
        var products = _context.Products
            .Where(x => 
                EF.Functions.Like(x.Title, "%" + searchText + "%") ||
                EF.Functions.Like(x.Tags, "%" + searchText + "%") 
                ).OrderBy(x => x.Title)
            .ToList();
        return View("Index", products);
    }

    public IActionResult ProductDetails(int id)
    {
        Product? product = _context.Products.FirstOrDefault(x => x.Id == id);
        if (product == null)
        {
            return NotFound();
        }
        var galeries = _context.ProductGaleries.Where(x => x.ProductId == id).ToList();
        ViewData["gallery"] = galeries;
        return View(product);
    }
    
}