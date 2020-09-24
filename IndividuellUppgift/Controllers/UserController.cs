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
            var requestSender = httpContext.HttpContext.User;
            if(requestSender.IsInRole(UserRoles.Admin) || requestSender.IsInRole(UserRoles.CEO))
            {
                var usersToGet = await dbContext.Users.ToListAsync();
                List<UserModel> users = GetUsersList(usersToGet);
                return Ok(users);
            }
            return Unauthorized();

        }

        [HttpGet]
        [Route("{username}")]
        [Authorize]
        public async Task<IActionResult> GetUser(string username)
        {
            var requestSender = httpContext.HttpContext.User;
            var userexists = await userManager.FindByNameAsync(username);
            var employee = nwContext.Employees.Find(userexists.EmpId);
            if(userexists == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { StatusCode = "Error", Message = "User does not exist" });
            }
            if (requestSender.IsInRole(UserRoles.Admin) || requestSender.IsInRole(UserRoles.CEO))
            {
                UserModel UserToGet = new UserModel()
                {
                    UserName = userexists.UserName,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Email = userexists.Email,
                    Country = employee.Country
                };
                return Ok(UserToGet);
            }
            return Unauthorized();
        }

        [HttpPut]
        [Route("update/{username}")]
        [Authorize]
        public async Task<IActionResult>UpdateUser(string userName, [FromBody]UserModel userModel)
        {
            
            var requestSender = httpContext.HttpContext.User;
            var userSender = await userManager.FindByNameAsync(requestSender.Identity.Name);
            var userToUpdate = await userManager.FindByNameAsync(userName);
            var employee = await nwContext.Employees.FindAsync(userSender.EmpId);
            if(userSender == null ||userToUpdate == null ||employee == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { StatusCode = "Error", Message = "User does not exist. Check your spelling" });
            }

            if(requestSender.IsInRole(UserRoles.Admin) || requestSender.Identity.Name == userSender.UserName)
            {
                if(userModel.UserName != null)
                {
                    userToUpdate.UserName = userModel.UserName;
                    userToUpdate.NormalizedUserName = userModel.UserName.ToUpper();
                }
                if (userModel.FirstName != null)
                    employee.FirstName = userModel.FirstName;
                if (userModel.LastName != null)
                    employee.LastName = userModel.LastName;
                if (userModel.Email != null)
                {
                    userToUpdate.Email = userModel.Email;
                    userToUpdate.NormalizedEmail = userModel.Email.ToUpper();
                }
                if (userModel.Country != null)
                    employee.Country = userModel.Country;
                if ((userModel.Role != null && await userManager.IsInRoleAsync(userSender, UserRoles.Admin)) && await roleManager.RoleExistsAsync(userModel.Role)) 
                    await userManager.AddToRoleAsync(userToUpdate, userModel.Role);

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
            var requestSender = httpContext.HttpContext.User;
             var user = await userManager.FindByNameAsync(username);
            if(user == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { StatusCode = "Error", Message = "User does not exist" });
            }
            if (requestSender.IsInRole(UserRoles.Admin))
            {
                await userManager.DeleteAsync(user);
                return Ok();
            }
            return Unauthorized();
        }

        private List<UserModel> GetUsersList(List<ApplicationUser> users)
        {
            var userList = new List<UserModel>();
            foreach (var user in users)
            {
                var employee = nwContext.Employees.Find(user.EmpId);
                var currentUser = new UserModel();
                currentUser.Email = user.Email;
                currentUser.UserName = user.UserName;
                currentUser.FirstName = employee.FirstName;
                currentUser.LastName = employee.LastName;
                currentUser.Country = employee.Country;
                userList.Add(currentUser);
            }
            return userList;
        }
    }
}
