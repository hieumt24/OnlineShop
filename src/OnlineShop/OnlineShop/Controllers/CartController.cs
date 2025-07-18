using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OnlineShop.Models.Db;
using OnlineShop.Models.ViewModels;

namespace OnlineShop.Controllers;

public class CartController : Controller
{
    private readonly OnlineShopContext _context;

    public CartController(OnlineShopContext context)
    {
        _context = context;
    }

    [HttpPost]
    public IActionResult UpdateCard([FromBody] CartViewModel request)
    {
        var product = _context.Products.FirstOrDefault(x => x.Id == request.ProductId);
        
        if(product == null)
            return NotFound();
        
        // Retrieve the list of products in the cart using the dedicated fucntion
        var cartItems = GetCartItem();
        
        var foundProductInCart = cartItems.FirstOrDefault(x => x.ProductId == request.ProductId);

        if (foundProductInCart == null)
        {
            var newCartItem = new CartViewModel() { };
            newCartItem.ProductId = request.ProductId;
            newCartItem.Count = request.Count;
            
            cartItems.Add(newCartItem);
        }
        else
        {
            if (request.Count > 0)
            {
                foundProductInCart.Count = request.Count + 1;
            }
            else
            {
                cartItems.Remove(foundProductInCart);
            }
        }
        
        var json = JsonConvert.SerializeObject(cartItems);
        
        CookieOptions options = new CookieOptions();
        options.Expires = DateTime.Now.AddDays(7);
        Response.Cookies.Append("Cart", json, options);

        var result = cartItems.Sum(x => x.Count);
        return new JsonResult(result);
    }

    public List<CartViewModel> GetCartItem()
    {
        List<CartViewModel> cartList = new List<CartViewModel>();
        
        var prevCartItemString = Request.Cookies["Cart"];
        if (!string.IsNullOrEmpty(prevCartItemString))
        {
            cartList = JsonConvert.DeserializeObject<List<CartViewModel>>(prevCartItemString);
        }
        
        return cartList;
    }

    public IActionResult SmallCart()
    {
        var cartItems = GetCartItem();
        if (!cartItems.Any())
        {
            return PartialView(null);
        }
        var cartItemProductIds = cartItems.Select(x => x.ProductId).ToList();
        
        // Load products into memory
        var products = _context.Products
            .Where(x => cartItemProductIds.Contains(x.Id))
            .ToList();
        
        // Combine cart items with product details
        List<ProductCartViewModel> productCartViewModels = new List<ProductCartViewModel>();
    
        foreach (var item in products)
        {
            var newItem = new ProductCartViewModel()
            {
                Id = item.Id,
                ImageName = item.ImageName,
                Price = item.Price - (item.Discount ?? 0),
                Title = item.Title,
                Count = cartItems.Single(x => x.ProductId == item.Id).Count,
                RowSumPrice = (item.Price - (item.Discount ?? 0)) * cartItems.Single(x => x.ProductId == item.Id).Count
            };
            productCartViewModels.Add(newItem);
        }
        return PartialView(productCartViewModels);
    }
}