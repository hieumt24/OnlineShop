using Microsoft.AspNetCore.Mvc;

namespace OnlineShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomesController : Controller
    {
        // GET: HomesController
        public ActionResult Index()
        {
            return View();
        }

    }
}
