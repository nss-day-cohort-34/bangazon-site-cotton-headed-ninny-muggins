﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Bangazon.Data;
using Bangazon.Models;
using Bangazon.Models.ProductViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;


namespace Bangazon.Controllers
{
   
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _context = context;
        }

        private Task<ApplicationUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

        public async Task<IActionResult> Types()
        {
            //var model = new ProductTypesViewModel();
            var groupedProducts = await _context
                .ProductType
                .Select(pt => new GroupedProducts
                    {
                        TypeId = pt.ProductTypeId,
                        TypeName = pt.Label,
                        ProductCount = pt.Products.Count(),
                        Products = pt.Products.OrderByDescending(p => p.DateCreated).Take(3)
                    }).ToListAsync();
            return View(groupedProducts);
        }


        // GET: Products
        [Authorize]
        public async Task<IActionResult> ToSellIndex()
        {
            var user = await GetCurrentUserAsync();
            var products = await _context.Product
                                        .Include(p => p.ProductType)
                                        .Where(p => p.UserId == user.Id).ToListAsync();
            //1. need to count the number of product solds 
            //number of orders for a given product on orders tht have payment type 
            foreach (var product in products)
            {
                var userOrders = _context.OrderProduct.Where(op => op.Order.DateCompleted != null);
                var productsSold = userOrders.Select(op => op.ProductId).Where(id => id == product.ProductId).Count();
                product.ProductSold = productsSold;
            }
                
            return View(products);
        }


        
        //GET: ProductTypes with products
        public async Task<IActionResult> ProductListByType(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var productType = _context.ProductType
                .Where(pt => pt.ProductTypeId == id)
                .Include(pt => pt.Products).ToList()
                .FirstOrDefault();
               
            if (productType == null)
            {
                return NotFound();
            }

            return View(productType);
        }


        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Product
                .Include(p => p.ProductType)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        [Authorize]
        // GET: Products/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new ProductCreateAndEditViewModel()
            {
                ProductTypes = await _context.ProductType.ToListAsync()
            };
            return View(viewModel);
        }

        // POST: Products/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateAndEditViewModel viewModel)
        {
            ModelState.Remove("Product.UserId");
            ModelState.Remove("Product.User");
            if (ModelState.IsValid)
            {
                if (hasSpecialChar(viewModel.Product.Title) || hasSpecialChar(viewModel.Product.Description))
                {
                    TempData["notice"] = "Product title and description cannot contain special characters (!@#$%^()&*).";
                    viewModel.ProductTypes = await _context.ProductType.ToListAsync();               
                    return View(viewModel);
                }
                if (viewModel.Product.Price > 10000)
                {
                    TempData["maxPrice"] = "Price cannot exceed $10,000.";
                    viewModel.ProductTypes = await _context.ProductType.ToListAsync();
                    return View(viewModel);
                }
                var user = await GetCurrentUserAsync();
                viewModel.Product.User = user;
                viewModel.Product.UserId = user.Id;
                viewModel.Product.DateCreated = DateTime.Now;
                _context.Add(viewModel.Product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ToSellIndex));
            }

             
            return View(viewModel);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var viewModel = new ProductCreateAndEditViewModel()
            {
                Product = await _context.Product.FindAsync(id),
                ProductTypes = await _context.ProductType.ToListAsync()
            };

            if (viewModel.Product == null)
            {
                return NotFound();
            }

            return View(viewModel);
        }
        // POST: Products/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductCreateAndEditViewModel viewModel)
        {
            if (id != viewModel.Product.ProductId)
            {
                return NotFound();
            }
            ModelState.Remove("Product.UserId");
            ModelState.Remove("Product.User");
            if (ModelState.IsValid)
            {
                try
                {
                    if (hasSpecialChar(viewModel.Product.Title) || hasSpecialChar(viewModel.Product.Description))
                    {
                        TempData["notice"] = "Product title and description cannot contain special characters (!@#$%^()&*).";
                        viewModel.ProductTypes = await _context.ProductType.ToListAsync();
                        return View(viewModel);
                    }
                    if (viewModel.Product.Price > 10000)
                    {
                        TempData["maxPrice"] = "Price cannot exceed $10,000.";
                        viewModel.ProductTypes = await _context.ProductType.ToListAsync();
                        return View(viewModel);
                    }
                    var user = await GetCurrentUserAsync();
                    viewModel.Product.User = user;
                    viewModel.Product.UserId = user.Id;
                    _context.Update(viewModel.Product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(viewModel.Product.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ToSellIndex));
            }

            return View(viewModel);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Product
                .Include(p => p.ProductType)
                .Include(p => p.User)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Product.FindAsync(id);
            _context.Product.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ToSellIndex));
        }

        private bool ProductExists(int id)
        {
            return _context.Product.Any(e => e.ProductId == id);
        }

        public bool hasSpecialChar(string input)
        {
            string specialChar = @"!@#$%^&*()";
            foreach (var item in specialChar)
            {
                if (input.Contains(item)) return true;
            }

            return false;
        }
    }
}
