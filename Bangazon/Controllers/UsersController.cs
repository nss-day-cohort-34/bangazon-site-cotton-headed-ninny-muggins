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
using Microsoft.AspNetCore.Authorization;

namespace Bangazon.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private Task<ApplicationUser> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var User = new User()
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                StreetAddress = user.StreetAddress,
                PhoneNumber = user.PhoneNumber,
                ApplicationUser = new ApplicationUser()
                {
                    Orders = user.Orders,
                    PaymentTypes = user.PaymentTypes
                }
            };
            //var applicationDbContext = _context.User
            //                                    .Include(u => u.FirstName)
            //                                    .Where(u => u.UserId == u.UserId);
            return View(User);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details()
        {
            //if (id == null)
            //{
            //    return NotFound();
            //}

            var user = await _userManager.GetUserAsync(HttpContext.User);
            var User = new User()
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                StreetAddress = user.StreetAddress,
                PhoneNumber = user.PhoneNumber
            };
            if (User == null)
            {
                return NotFound();
            }

            return View(User);
        }

        // GET: Users/Create
        //public IActionResult Create()
        //{
        //    return View();
        //}

        //// POST: Users/Create
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        //// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("UserId,FirstName,LastName,StreetAddress")] User user)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _context.Add(user);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(user);
        //}

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(HttpContext.User);
            var User = new User()
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                StreetAddress = user.StreetAddress
            };
            if (User == null)
            {
                return NotFound();
            }
            return View(User);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("UserId,FirstName,LastName,StreetAddress")] User userModel)
        {
            //edit does not update the db on post so it fails
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var User = new User()
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                StreetAddress = user.StreetAddress
            };

            if (user.Id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(User);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
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
           ViewData["UserId"] = new SelectList(_context.ApplicationUsers, "Id", "Id", userModel.UserId);
            return View(userModel);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.User
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _context.User.FindAsync(id);
            _context.User.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(string id)
        {
            return _context.User.Any(e => e.UserId == id);
        }
    }
}
