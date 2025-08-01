using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Hubs;
using OnlineShop.Models.Db;

namespace OnlineShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly OnlineShopContext _context;
        private readonly IHubContext<ProductHub> _hubContext;

        public ProductsController(OnlineShopContext context, IHubContext<ProductHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.ToListAsync());
        }

        public IActionResult DeleteGallery(int id)
        {
            var gallery = _context.ProductGaleries.FirstOrDefault(x => x.Id == id);
            if (gallery == null)
            {
                return NotFound();
            }

            var directory = Directory.GetCurrentDirectory();
            var path = directory + "\\wwwroot\\images\\banners\\" + gallery.ImageName;
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            _context.ProductGaleries.Remove(gallery);
            _context.SaveChanges();
            _hubContext.Clients.All.SendAsync("GalleryDeleted", new
            {
                GalleryId = id,
                ProductId = gallery.ProductId
            });

            return Redirect("edit/" + gallery.ProductId);
        }

        // GET: Admin/Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Admin/Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Id,Title,Description,FullDesc,Price,Discount,ImageName,Qty,Tags,VideoUrl")] Product product,
            IFormFile? MainImage, IFormFile[]? GalleryImages)
        {
            if (ModelState.IsValid)
            {
                // ----- Saving main image ----- 
                if (MainImage != null)
                {
                    product.ImageName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(MainImage.FileName);
                    string fn;
                    fn = Directory.GetCurrentDirectory();
                    string ImagePath = Path.Combine(fn + "\\wwwroot\\images\\banners\\" + product.ImageName);

                    using (var stream = new FileStream(ImagePath, FileMode.Create))
                    {
                        MainImage.CopyTo(stream);
                    }
                }

                _context.Add(product);
                await _context.SaveChangesAsync();
                // =========== saving gallery images =================
                if (GalleryImages != null)
                {
                    foreach (var item in GalleryImages)
                    {
                        var newGallery = new ProductGalery();
                        newGallery.ProductId = product.Id;
                        // ----------------------
                        newGallery.ImageName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(item.FileName);
                        string fn;
                        fn = Directory.GetCurrentDirectory();
                        string ImagePath = Path.Combine(fn + "\\wwwroot\\images\\banners\\" + newGallery.ImageName);

                        using (var stream = new FileStream(ImagePath, FileMode.Create))
                        {
                            item.CopyTo(stream);
                        }

                        //----------------------
                        _context.ProductGaleries.Add(newGallery);
                    }
                }

                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("ProductCreated", new {
                    id          = product.Id,
                    title       = product.Title,
                    description = product.Description,
                    price       = product.Price,
                    discount    = product.Discount,
                    qty         = product.Qty,
                    tags        = product.Tags,
                    imageName   = product.ImageName,
                    createdAt   = DateTime.Now
                });

                // Refresh statistics
                await RefreshStatisticsForAllClients();
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var galeries = _context.ProductGaleries.Where(x => x.ProductId == id).ToList();
            ViewData["gallery"] = galeries;
            return View(product);
        }

        // POST: Admin/Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,Title,Description,FullDesc,Price,Discount,ImageName,Qty,Tags,VideoUrl")] Product product,
            IFormFile? MainImage, IFormFile[]? GalleryImages)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // ----- Saving main image -----
                    if (MainImage != null)
                    {
                        string directory = Directory.GetCurrentDirectory();
                        string path = directory + "\\wwwroot\\images\\banners\\" + product.ImageName;
                        if (System.IO.File.Exists(path))
                        {
                            System.IO.File.Delete(path);
                        }

                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            MainImage.CopyTo(stream);
                        }
                    }

                    if (GalleryImages != null)
                    {
                        foreach (var item in GalleryImages)
                        {
                            var imageItem = Guid.NewGuid() + Path.GetExtension(item.FileName);

                            //
                            string directory = Directory.GetCurrentDirectory();
                            string path = directory + "\\wwwroot\\images\\banners\\" + imageItem;
                            using (var stream = new FileStream(path, FileMode.Create))
                            {
                                item.CopyTo(stream);
                            }

                            var galleryItem = new ProductGalery();
                            galleryItem.ImageName = imageItem;
                            galleryItem.ProductId = product.Id;

                            _context.ProductGaleries.Add(galleryItem);
                        }
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.All.SendAsync("ProductUpdated", new
                    {
                        Id = product.Id,
                        Title = product.Title,
                        Description = product.Description,
                        Price = product.Price,
                        ImageName = product.ImageName,
                        UpdatedAt = DateTime.Now
                    });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        // GET: Admin/Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // ============= delete images =====================
                string directory = Directory.GetCurrentDirectory();
                string fn = directory + "\\wwwroot\\images\\banners\\";
                string mainImagePath = fn + product.ImageName;
                if (System.IO.File.Exists(mainImagePath))
                {
                    System.IO.File.Delete(mainImagePath);
                }

                // delete gallery images
                var galleries = _context.ProductGaleries.Where(x => x.ProductId == id).ToList();
                if (galleries.Count > 0)
                {
                    foreach (var item in galleries)
                    {
                        string galleryImagePath = fn + item.ImageName;
                        if (System.IO.File.Exists(galleryImagePath))
                        {
                            System.IO.File.Delete(galleryImagePath);
                        }
                    }

                    _context.ProductGaleries.RemoveRange(galleries);
                }

                //============================================
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ProductDeleted", new
            {
                Id = id,
                Title = product.Title,
                DeletedAt = DateTime.Now
            });
            await RefreshStatisticsForAllClients();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }

        [HttpGet]
        public async Task<IActionResult> GetProductsRealtime()
        {
            var products = await _context.Products.ToListAsync();
            return Json(products);
        }

        // API endpoint để lấy thông tin sản phẩm theo ID
        [HttpGet]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            return Json(product);
        }

        // API endpoint để cập nhật số lượng sản phẩm realtime
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.Qty = quantity;
            _context.Update(product);
            await _context.SaveChangesAsync();

            // Thông báo realtime về việc cập nhật số lượng
            await _hubContext.Clients.All.SendAsync("QuantityUpdated", new
            {
                ProductId = id,
                NewQuantity = quantity,
                UpdatedAt = DateTime.Now
            });

            return Json(new { success = true, message = "Quantity updated successfully" });
        }

        private async Task RefreshStatisticsForAllClients()
        {
            try
            {
                var totalProducts = await _context.Products.CountAsync();
                await _hubContext.Clients.All.SendAsync("StatisticsUpdated", new
                {
                    TotalProducts = totalProducts,
                    ActiveUsers = 1,
                    Status = "Connected"
                });
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error refreshing statistics: {ex.Message}");
            }
        }
    }
}