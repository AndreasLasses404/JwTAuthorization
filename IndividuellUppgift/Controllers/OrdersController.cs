using IndividuellUppgift.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NorthwindDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IndividuellUppgift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _dbContext;
        private readonly NorthwindContext _nwContext;
        private readonly IHttpContextAccessor _httpContext;
        public OrdersController(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, NorthwindContext nwContext, IHttpContextAccessor httpContext)
        {
            this._userManager = userManager;
            _dbContext = dbContext;
            _nwContext = nwContext;
            _httpContext = httpContext;
        }


        [HttpGet]
        [Route("getmyorders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var requestee = _httpContext.HttpContext.User;
            var user = await _userManager.FindByNameAsync(requestee.Identity.Name);
            var employee = await _nwContext.Employees.Include(e => e.Orders).FirstOrDefaultAsync(x => x.EmployeeId == user.EmpId);

            var query = employee.Orders.ToList();

            return Ok(query);
        }
    }
}
