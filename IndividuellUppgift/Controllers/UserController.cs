using IndividuellUppgift.Authentication;
using IndividuellUppgift.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NorthwindDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace IndividuellUppgift.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IHttpContextAccessor httpContext;
        private readonly ApplicationDbContext dbContext;
        private readonly NorthwindContext nwContext;
        private ISession session => httpContext.HttpContext.Session;

        public UserController(ApplicationDbContext appContext, NorthwindContext northwindContext, UserManager<ApplicationUser> uManager, RoleManager<IdentityRole>rManager, IHttpContextAccessor http)
        {
            this.userManager = uManager;
            this.roleManager = rManager;
            httpContext = http;
            dbContext = appContext;
            nwContext = northwindContext;
        }

        [HttpGet]
        [Route("getall")]
        public async Task<IActionResult> GetUsers()
        {
            var employees = nwContext.Employees.ToListAsync();
            var users = await dbContext.Users.Include(u => u.employee).ToListAsync();
            List<UserModel> readUsers = new List<UserModel>();
            foreach(var user in users)
            {
               var readuser = new UserModel();
                readuser.Email = user.Email;
                readuser.UserName = user.UserName;
                readuser.FirstName = user.employee.FirstName;
                readuser.LastName = user.employee.LastName;
                readuser.Country = user.employee.Country;
                readUsers.Add(readuser);
            }
            return new JsonResult(readUsers);
        }

        [HttpGet]
        [Route("{username}")]
        public async Task<IActionResult> GetUser(string username)
        {
            var userexists = await userManager.FindByNameAsync(username);
            if(userexists == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { StatusCode = "Error", Message = "User does not exist. Check you spelling" });
            }
            var user = await userManager.Users
                .Include(u => u.employee)
                .SingleAsync(u => u.UserName == username);
            UserModel readUser = new UserModel()
            {
                UserName = user.UserName,
                FirstName = user.employee.FirstName,
                LastName = user.employee.LastName,
                Email = user.Email,
                Country = user.employee.Country
            };
            
            return new JsonResult(readUser);

        }

        [HttpPatch]
        [Route("{username}")]
        public async Task<IActionResult>UpdateUser(string username, [FromBody]UserModel userModel)
        {
            var userToUpdate = await userManager.Users
                .Include(u => u.employee).SingleAsync(user => user.UserName == username);

            userToUpdate.UserName = userModel.UserName;
            userToUpdate.employee.FirstName = userModel.FirstName;
            userToUpdate.employee.LastName = userModel.LastName;
            userToUpdate.Email = userModel.Email;
            userToUpdate.employee.Country = userModel.Country;
            userToUpdate.NormalizedEmail = userModel.Email.ToUpper();
            userToUpdate.NormalizedUserName = userModel.UserName.ToUpper();

            await dbContext.SaveChangesAsync();

            return Ok();
        }
    }
}
