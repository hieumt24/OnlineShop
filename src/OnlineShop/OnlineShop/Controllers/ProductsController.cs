using System.Text.RegularExpressions;
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

        ViewData["NewProducts"] = _context.Products.Where(x => x.Id != id)
            .Take(6).OrderByDescending(x => x.Id).ToList();
        
        var comments = _context.Comments.Where(x => x.ProductId == id).ToList();

        ViewData["comments"] = _context.Comments.Where(x => x.ProductId == id)
            .OrderByDescending(x => x.CreateDate).ToList();
        
        // Calculate average rating
        double averageRating = 0;
        if (comments.Any())
        {
            averageRating = comments.Average(x => x.Rating);
        }
        ViewData["AverageRating"] = averageRating;
        
        return View(product);
    }

    [HttpPost]
    public IActionResult SubmitComment(string name, string email, string commentText, int productId, int rating)
    {
        if (!string.IsNullOrEmpty(name) &&
            !string.IsNullOrEmpty(email) &&
            !string.IsNullOrEmpty(commentText) &&
            productId > 0)
        {
            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match math = regex.Match(email);
            if (!math.Success)
            {
                TempData["ErrorMessage"] = "Email không hợp lệ";
                return Redirect("/Products/ProductDetails/" + productId);
            }

            Comment newComment = new Comment()
            {
                Name = name,
                Email = email,
                CommentText = commentText,
                ProductId = productId,
                Rating = rating,
                CreateDate = DateTime.Now
            };

            _context.Comments.Add(newComment);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi bình luận";
            return Redirect("/Products/ProductDetails/" + productId);
        }

        TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin";
        return Redirect("/Products/ProductDetails/" + productId);
    }
}