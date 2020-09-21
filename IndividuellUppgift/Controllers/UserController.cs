using IndividuellUppgift.Authentication;
using IndividuellUppgift.Models;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize]
        public async Task<IActionResult> GetUsers()
        {

            //SKAPA AUTHORIZATION HÄR
            var request = httpContext.HttpContext.User;
            if(request.IsInRole(UserRoles.Admin) || request.IsInRole(UserRoles.CEO))
            {
                var users = await dbContext.Users.ToListAsync();
                List<UserModel> readUsers = new List<UserModel>();
                foreach (var user in users)
                {
                    var employee = nwContext.Employees.Find(user.EmpId);
                    var readuser = new UserModel();
                    readuser.Email = user.Email;
                    readuser.UserName = user.UserName;
                    readuser.FirstName = employee.FirstName;
                    readuser.LastName = employee.LastName;
                    readuser.Country = employee.Country;
                    readUsers.Add(readuser);
                }
                return new JsonResult(readUsers);
            }
            return Unauthorized();

        }

        [HttpGet]
        [Route("{username}")]
        [Authorize]
        public async Task<IActionResult> GetUser(string username)
        {
            var request = httpContext.HttpContext.User;
            var userexists = await userManager.FindByNameAsync(username);
            var employee = nwContext.Employees.Find(userexists.EmpId);
            if(userexists == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { StatusCode = "Error", Message = "User does not exist. Check you spelling" });
            }
            if (request.IsInRole(UserRoles.Admin) || request.IsInRole(UserRoles.CEO))
            {
                UserModel readUser = new UserModel()
                {
                    UserName = userexists.UserName,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Email = userexists.Email,
                    Country = employee.Country
                };
                return new JsonResult(readUser);
            }

            return Unauthorized();
        }

        [HttpPatch]
        [Route("{username}")]
        [Authorize]
        public async Task<IActionResult>UpdateUser(string username, [FromBody]UserModel userModel)
        {
            var request = httpContext.HttpContext.User;
            var userExists = await userManager.FindByNameAsync(username);
            var employee = await nwContext.Employees.FindAsync(userExists.EmpId);
            if(userExists == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { StatusCode = "Error", Message = "User does not exist. Check your spelling" });
            }

            if(request.IsInRole(UserRoles.Admin) || request.Identity.Name == userExists.UserName)
            {
                if(userModel.UserName != null)
                {
                    userExists.UserName = userModel.UserName;
                    userExists.NormalizedUserName = userModel.UserName.ToUpper();
                }
                if (userModel.FirstName != null)
                    employee.FirstName = userModel.FirstName;
                if (userModel.LastName != null)
                    employee.LastName = userModel.LastName;
                if (userModel.Email != null)
                {
                    userExists.Email = userModel.Email;
                    userExists.NormalizedEmail = userModel.Email.ToUpper();
                }
                if (userModel.Country != null)
                    employee.Country = userModel.Country;
                


                await dbContext.SaveChangesAsync();

                return Ok();
            }
            return Unauthorized();
        }

        [HttpDelete]
        [Route("delete/{username}")]
        [Authorize]
        public async Task<IActionResult>DeleteUser(string username)
        {
            var request = httpContext.HttpContext.User;
             var user = await userManager.FindByNameAsync(username);
            if(user == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { StatusCode = "Error", Message = "User does not exist. Check your spelling" });
            }
            if (request.IsInRole(UserRoles.Admin))
            {
                await userManager.DeleteAsync(user);

                return Ok();
            }
            return Unauthorized();
        }
    }
}
