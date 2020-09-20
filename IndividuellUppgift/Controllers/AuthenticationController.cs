using IndividuellUppgift.Authentication;
using IndividuellUppgift.Models;
using NorthwindDb;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace IndividuellUppgift.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly NorthwindContext nwContext;
        private readonly ApplicationDbContext appContext;


        public AuthenticationController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration, NorthwindContext dbContext, ApplicationDbContext idContext) : base()
        {
            this._userManager = userManager;
            this._roleManager = roleManager;
            _configuration = configuration;
            nwContext = dbContext;
            appContext = idContext;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> RegisterUser([FromBody]RegisterModel user)
        {
            var existingUser = await _userManager.FindByNameAsync(user.UserName);
            if(existingUser != null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { StatusCode = "Error", Message = "User already exists" });
            }
            ApplicationUser newUser = new ApplicationUser()
            {
                UserName = user.UserName,
                Email = user.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                employee = (Employees)nwContext.Employees.Find(user.EmpId)
                
            };
            newUser.employee.EmployeeId = 0;



            if(!await _roleManager.RoleExistsAsync(UserRoles.Admin))
            {
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));
            }
            if (!await _roleManager.RoleExistsAsync(UserRoles.CEO))
            {
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.CEO));
            }
            if (!await _roleManager.RoleExistsAsync(UserRoles.CountryManager))
            {
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.CountryManager));
            }
            if (!await _roleManager.RoleExistsAsync(UserRoles.Employee))
            {
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Employee));
            }

            if(await _roleManager.RoleExistsAsync(user.Role))
            {
                var createUser = await _userManager.CreateAsync(newUser, user.Password);
                if (!createUser.Succeeded)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new Response { StatusCode = "Error", Message = "User creation failed! Please check user details and try again." });
                }
                await _userManager.AddToRoleAsync(newUser, user.Role);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { StatusCode = "Error", Message = "The role you entered is not valid. Try again" });
            }


            return Ok(new Response { StatusCode = "Success", Message = "User has been created Successfully" });
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody]LoginModel loginUser)
        {
            var user = await _userManager.FindByNameAsync(loginUser.UserName);
            if(user != null && await _userManager.CheckPasswordAsync(user, loginUser.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Country, user.employee.Country)
                };
                

                foreach(var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }
                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

                var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    expires: DateTime.Now.AddMinutes(30),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                    );
                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo

                    
                });
            }
            return Unauthorized();
        }

        //public async Task<IActionResult> TokenRefresh()
        //{

        //}

    }
}
