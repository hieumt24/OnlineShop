using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OnlineShop.Models;
using OnlineShop.Models.Db;
using OnlineShop.Services;
using OnlineShop.Services.Interfaces;

namespace OnlineShop.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly OnlineShopContext _context;
    private readonly IProductService _productService;

    public HomeController(ILogger<HomeController> logger, OnlineShopContext context, IProductService productService)
    {
        _logger = logger;
        _context = context;
        _productService = productService;
    }

    public IActionResult Index()
    {
        var banners = _context.Banners.ToList();
        ViewData["banners"] = banners;
        // ------ new products ------
        var newProducts = _context.Products.OrderByDescending(x => x.Id).Take(8).ToList();
        ViewData["newProducts"] = newProducts;
        
        var comments = _context.Comments.ToList();
        ViewData["comments"] = comments;
        
        var bestSellingProducts = _productService.GetBestSellingProducts(10);
        ViewData["bestSellingProducts"] = bestSellingProducts;
        
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}