using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Bangazon.Data;
using Bangazon.Models;
using Microsoft.AspNetCore.Identity;
using Bangazon.Models.OrderViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Bangazon.Controllers
{
    public class OrdersController : Controller
    {
        private readonly IConfiguration _config;

        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration config)
        {
            _config = config;
            _context = context;
            _userManager = userManager;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        private Task<ApplicationUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var pastOrders = _context.Order
                .Where(o => o.DateCompleted != null)
                .Include(o => o.PaymentType)
                .Include(o => o.User);
            return View(await pastOrders.ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details()
        {
            var order = await _context.Order
                .Include(o => o.PaymentType)
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.DateCompleted == null);

            if (order == null)
            {
                var emptyOrderDetail = new OrderDetailViewModel()
                {
                    Order = new Order(),
                    OrderProducts = new List<OrderProduct>(),
                };
                return View(emptyOrderDetail);
            }
            else
            {
                var orderProduct = await _context.OrderProduct
                    .Where(op => op.OrderId == order.OrderId)
                    .Include(op => op.Product)
                    .ToListAsync();
                var lineItems = orderProduct.Select(op =>
                {
                    var olm = new OrderLineItem
                    {
                        Product = op.Product,
                        Units = 1
                    };
                    return olm;
                });
                var orderDetail = new OrderDetailViewModel()
                {
                    Order = order,
                    OrderProducts = orderProduct,
                    LineItems = lineItems
                };
                return View(orderDetail);
            }
        }

        // GET: Orders/Create
        //   public async Task<IActionResult> Create()
        //{
        // var existingOrder = await _context.Order
        //     .Where(o => o.DateCompleted == null)
        //     .Include(o => o.PaymentType)
        //     .Include(o => o.User).ToListAsync();
        //// return View(await pastOrders.ToListAsync());

        // if (existingOrder != null)
        // {
        //     existingOrder.OrderProducts.Add()
        // }

        // return View();
        //  }

        // POST: Orders/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productId)
        {
            var existingOrder = await _context.Order
      .Where(o => o.DateCompleted == null)
      .Include(o => o.PaymentType)
      .Include(o => o.User)
      .FirstOrDefaultAsync();
            // return View(await pastOrders.ToListAsync());


            if (existingOrder != null)
            {
                var orderProduct = new OrderProduct { OrderId = existingOrder.OrderId, ProductId = productId };
                _context.OrderProduct.Add(orderProduct);
                await _context.SaveChangesAsync();
                TempData["cart-notice"] = "Item successfully added to cart!";
                //might need to change the view
                return RedirectToAction("Details", "Products", new { Id = productId });
            }

            else
            {
                    var user = await GetCurrentUserAsync();

                    // 1. get the id of the new posted Order
                    using (SqlConnection conn = Connection)
                    {
                        conn.Open();
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandText = @"INSERT INTO [Order]
                                            ( UserId, PaymentTypeId, DateCreated, DateCompleted)
                                            OUTPUT Inserted.OrderId
                                            VALUES
                                            ( @UserId, null, GETDATE(), null);
                                            ";
                            cmd.Parameters.Add(new SqlParameter("@UserId", user.Id));
                            int orderId = (Int32)cmd.ExecuteScalar();

                            cmd.CommandText = @"INSERT INTO OrderProduct
                                            ( OrderId, ProductId )
                                            VALUES
                                            ( @OrderId, @ProductId );
                                            ";
                            cmd.Parameters.Add(new SqlParameter("@OrderId", orderId));
                            cmd.Parameters.Add(new SqlParameter("@ProductId", productId));
                            cmd.ExecuteNonQuery();
                        }
                    }
                    TempData["cart-notice"] = "Item successfully added to cart!";

                    return RedirectToAction("Details", "Products", new { Id = productId });
            }
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            ViewData["PaymentTypeId"] = new SelectList(_context.PaymentType, "PaymentTypeId", "AccountNumber", order.PaymentTypeId);
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id", order.UserId);
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OrderId,DateCreated,DateCompleted,UserId,PaymentTypeId")] Order order)
        {
            if (id != order.OrderId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.OrderId))
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
            ViewData["PaymentTypeId"] = new SelectList(_context.PaymentType, "PaymentTypeId", "AccountNumber", order.PaymentTypeId);
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id", order.UserId);
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .Include(o => o.PaymentType)
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Order o)
        {
            var order = await _context.Order.FindAsync(o.OrderId);
            var orderProducts = await _context.OrderProduct.Where(op => op.OrderId == order.OrderId).ToListAsync();

            foreach (var op in orderProducts)
            {
                _context.OrderProduct.Remove(op);
            }
            _context.Order.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> DeleteOrderProduct(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderProduct = await _context.OrderProduct
                .Include(op => op.Order)
                .Include(op => op.Product)
                .FirstOrDefaultAsync(op => op.OrderProductId == id);
            //                .Include(o => o.OrderProducts);
            //.Where(op => op.OrderId == o.OrderId));
            // .Include(o => o.PaymentType)

            if (orderProduct == null)
            {
                return NotFound();
            }

            return View(orderProduct);
        }

        //POST: Orders/Delete/5
        [HttpPost, ActionName("DeleteOP")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrderProductConfirmed(OrderProduct op)
        {
            //var orderProduct = await _context.OrderProduct.FindAsync(id);
            _context.OrderProduct.Remove(op);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details));
        }

        private bool OrderExists(int id)
        {
            return _context.Order.Any(e => e.OrderId == id);
        }
    }
}
