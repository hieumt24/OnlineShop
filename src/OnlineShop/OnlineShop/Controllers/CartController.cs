using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
    
    // GET
    public IActionResult Index()
    {
        var result = GetProductInCart();
        return View(result);
    }
    
    public IActionResult ClearCart()
    {
        Response.Cookies.Delete("Cart");
        return Redirect("/");
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
                foundProductInCart.Count = request.Count;
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

    public List<ProductCartViewModel> GetProductInCart()
    {
        var cartItems = GetCartItem();
        if (!cartItems.Any())
        {
            return null;
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
        return  productCartViewModels;
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
        var result = GetProductInCart();
        return PartialView(result);
    }

    [Authorize]

    public IActionResult Checkout()
    {
        var order = new Order();
        
        var shipping = _context.Settings.First().Shipping;
        if (shipping != null)
        {
            order.Shipping = shipping;
        }

        ViewData["Products"] = GetProductInCart();

        return View(order);
    }

    [Authorize]
    [HttpPost]
    public IActionResult ApplyCouponCode([FromForm] string couponCode)
    {
        var order = new Order();
        
        var coupon = _context.Coupons.FirstOrDefault(c => c.Code == couponCode);
        
        var shipping = _context.Settings.First().Shipping;

        if (coupon != null)
        {
            order.CouponCode = coupon.Code;
            order.CouponDiscount = coupon.Discount;
            TempData["success"] = "Mã giảm giá đã được áp dụng thành công.";
        }
        else
        {
            ViewData["Products"] = GetProductInCart();
            TempData["message"] = "Mã giảm giá không hợp lệ hoặc đã hết hạn.";
            if(shipping != null)
            {
                order.Shipping = shipping;
            }
            return View("Checkout", order);
            
        }
        
        if(shipping != null)
        {
            order.Shipping = shipping;
        }

        ViewData["Products"] = GetProductInCart();
        
        return View("Checkout", order);
    }

    [Authorize]
    [HttpPost]
    public IActionResult Checkout(Order order)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Products"] = GetProductInCart();

            return View(order);
        }
        
        //check and find coupon
        if (!string.IsNullOrEmpty(order.CouponCode))
        {
            var coupon = _context.Coupons.FirstOrDefault(c => c.Code == order.CouponCode);
            if (coupon != null)
            {
                order.CouponCode = coupon.Code;
                order.CouponDiscount = coupon.Discount;
            }
            else
            {
                TempData["messsage"] = "Mã giảm giá không hợp lệ hoặc đã hết hạn.";
                ViewData["Products"] = GetProductInCart();

                return View(order);
            }
        }

        var products = GetProductInCart();
        
        order.Shipping = _context.Settings.First().Shipping;
        order.CreateDate = DateTime.Now;
        order.SubTotal = products.Sum(x => x.RowSumPrice);
        order.Total = order.SubTotal + (order.Shipping ?? 0);
        order.UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        if (order.CouponDiscount != null)
        {
            order.Total -= order.CouponDiscount;
        }
        
        _context.Orders.Add(order);
        _context.SaveChanges();
        
        //------------------------------------------------------------
        
        List<OrderDetails> orderDetails = new List<OrderDetails>();

        foreach (var item in products)
        {
            OrderDetails orderDetailItem = new OrderDetails()
            {
                Count = item.Count,
                ProductTitle = item.Title,
                ProductPrice = (decimal)item.Price,
                OrderId = order.Id,
                ProductId = item.Id
            };
            orderDetails.Add(orderDetailItem);
        }
        
        _context.OrderDetails.AddRange(orderDetails);
        _context.SaveChanges();
        
        return Redirect("/Cart/RedirectToPayment?orderId=" + order.Id);
    }
    
}