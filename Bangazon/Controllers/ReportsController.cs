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
using Bangazon.Models.ReportViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace Bangazon.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _config = config;
        }
        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        private Task<ApplicationUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

        // GET: Reports
        public async Task<IActionResult> Index()
        {
            var order = _context.Order.Include(o => o.PaymentType).Include(o => o.User);
            return View(await order.ToListAsync());
        }

        // Get Incomplete orders
        public async Task<IActionResult> IncompleteOrders()
        {
            var user = await GetCurrentUserAsync();
            ViewData["UserId"] = user.Id;
            var viewModel = new IncompleteOrderViewModel();

            var incompleteOrders = await _context.Order
                .Include(o => o.User)
                .Include(o => o.OrderProducts)
                .ThenInclude(op => op.Product)
                .Where(o => o.DateCompleted == null && o.OrderProducts.Any(op => op.Product.User == user))
                .ToListAsync();

            viewModel.Orders = incompleteOrders;
            return View(viewModel);
        }

        //Multiple Orders
        public async Task<IActionResult> MultipleOrders()
        {
            var user = await GetCurrentUserAsync();
            var model = new MultipleOrderViewModel();

            model.MultipleOrdersList = await _context.ApplicationUsers
                              .Include(u => u.Orders)
                              .Where(u => u.Orders.Any(o => o.OrderProducts.Any(op => op.Product.User == user)))
                              .Where(u => u.Orders.Where(o => o.PaymentTypeId == null).Count() > 1)
                              .Select(u => new UserOrderCount
                                {
                                    User = u,
                                    OpenOrderNumber = u.Orders.Where(o => o.PaymentTypeId == null).Count()
                                })
                                .ToListAsync();

            return View(model);



        }

        //Abandoned ProductTypes
        public async Task<IActionResult> AbandonedProductTypes()
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    var user = await GetCurrentUserAsync();
                    cmd.CommandText = @"SELECT ProductTypeId, Label, COUNT(OrderId) as IncompleteOrderCount
                                        FROM (SELECT p.ProductTypeId, pt.Label, o.OrderId
                                                FROM [Order] o INNER JOIN OrderProduct op on op.OrderId = o.OrderId
                                                LEFT JOIN Product p on p.ProductId = op.ProductId
                                                LEFT JOIN ProductType pt on pt.ProductTypeId = p.ProductTypeId
                                                WHERE o.DateCompleted IS NULL and p.UserId = @userId
                                                GROUP BY o.OrderId, p.ProductTypeId, pt.Label) o 
                                                GROUP BY ProductTypeId, Label
                                                ORDER BY ProductTypeId";

                    cmd.Parameters.Add(new SqlParameter("@userId", user.Id));
                    
                    SqlDataReader reader = cmd.ExecuteReader();

                    var model = new AbandonedProductTypesReportViewModel();

                    while (reader.Read())
                    {
                        var newPT = new ProductType
                        {
                            ProductTypeId = reader.GetInt32(reader.GetOrdinal("ProductTypeId")),
                            Label = reader.GetString(reader.GetOrdinal("Label"))
                        };

                        var newProductTypeCount = new ProductTypeCount
                        {
                            ProductType = newPT,
                            IncompleteOrderCount = reader.GetInt32(reader.GetOrdinal("IncompleteOrderCount"))
                        };
                        model.IncompleteOrderCounts.Add(newProductTypeCount);
                    }
                    return View(model);
                }
            }
        }

        // GET: Reports/Details/5
        public async Task<IActionResult> Details(int? id)
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

        // GET: Reports/Create
        public IActionResult Create()
        {
            ViewData["PaymentTypeId"] = new SelectList(_context.PaymentType, "PaymentTypeId", "AccountNumber");
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id");
            return View();
        }

        // POST: Reports/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,DateCreated,DateCompleted,UserId,PaymentTypeId")] Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PaymentTypeId"] = new SelectList(_context.PaymentType, "PaymentTypeId", "AccountNumber", order.PaymentTypeId);
            ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id", order.UserId);
            return View(order);
        }

        // GET: Reports/Edit/5
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

        // POST: Reports/Edit/5
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

        // GET: Reports/Delete/5
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

        // POST: Reports/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Order.FindAsync(id);
            _context.Order.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Order.Any(e => e.OrderId == id);
        }
    }
}
