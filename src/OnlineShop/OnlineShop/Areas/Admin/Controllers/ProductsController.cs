using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Models.Db;

namespace OnlineShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductsController : Controller
    {
        private readonly OnlineShopContext _context;

        public ProductsController(OnlineShopContext context)
        {
            _context = context;
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
        public async Task<IActionResult> Create([Bind("Id,Title,Description,FullDesc,Price,Discount,ImageName,Qty,Tags,VideoUrl")] Product product, IFormFile? MainImage, IFormFile[]? GalleryImages)
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,FullDesc,Price,Discount,ImageName,Qty,Tags,VideoUrl")] Product product, IFormFile? MainImage, IFormFile[]? GalleryImages)
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
                            using(var stream = new FileStream(path, FileMode.Create))
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
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
