using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineShop.Models.Db;
using OnlineShop.Models.ViewModels;

namespace OnlineShop.Controllers;

public class AccountController : Controller
{
    private readonly OnlineShopContext _context;
    public AccountController(OnlineShopContext context)
    {
        _context = context;
    }
    // GET
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }
    [HttpPost]
    public IActionResult Register(User user)
    {
        user.RegisterDate = DateTime.Now;
        user.IsAdmin = false;
        user.Email = user.Email?.Trim();
        user.FullName = user.FullName?.Trim();
        user.Password = user.Password?.Trim();
        user.RecoverCode = 0;
        if (!ModelState.IsValid)
        {
            return View(user);
        }
        Regex emailRegex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
        Match match = emailRegex.Match(user.Email);
        if (!match.Success)
        {
            ModelState.AddModelError("Email", "Email không hợp lệ");
            return View(user);
        }
        var existingEmail = _context.Users.Any(x => x.Email == user.Email);
        if (existingEmail)
        {
            ModelState.AddModelError("Email", "Email đã được sử dụng");
            return View(user);
        }
        
        _context.Users.Add(user);
        _context.SaveChanges();
        return RedirectToAction("login");
    }
    
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(LoginViewModel request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }
        var foundUser = _context.Users.FirstOrDefault(x => x.Email == request.Email.Trim() && x.Password == request.Password.Trim());
        if (foundUser == null)
        {
            ModelState.AddModelError("Email", "Email hoặc mật khẩu không đúng");
            return View(request);
        }
        // Create claims for authenticed user
        var claims = new List<Claim>();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, foundUser.Id.ToString()));
        claims.Add(new Claim(ClaimTypes.Name, foundUser.FullName));
        claims.Add(new Claim(ClaimTypes.Email, foundUser.Email));
        //
        if (foundUser.IsAdmin == true)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.Role, "User"));
        }
        //Create an identity based on the claims
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        
        //Create a principal based on the identity
        var principal = new ClaimsPrincipal(identity);
        
        //Sign in the user
        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Redirect("/");
    }

    [Authorize]
    public IActionResult Logout()
    {
        HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }
}