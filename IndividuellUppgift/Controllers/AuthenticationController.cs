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
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using AutoMapper;
using Microsoft.VisualBasic.CompilerServices;

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
        [AllowAnonymous]
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
                EmpId = user.EmpId
            };

            if (!await _roleManager.RoleExistsAsync(UserRoles.Admin))
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
            var createUser = await _userManager.CreateAsync(newUser, user.Password);
            if (!createUser.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { StatusCode = "Error", Message = "User creation failed! Please check user details and try again." });
            }
            var admins = await _userManager.GetUsersInRoleAsync(UserRoles.Admin);
            if(admins.Count == 0)
            {
                await _userManager.AddToRoleAsync(newUser, UserRoles.Admin);
            }
            else
            {
                await _userManager.AddToRoleAsync(newUser, UserRoles.Employee);
            }
            var emp = nwContext.Employees.Where(u => u.EmployeeId == newUser.EmpId).FirstOrDefault();
            if(emp != null)
            {
                await appContext.SaveChangesAsync();

                return Ok(new AuthenticateResponse()
                {
                    UserName = newUser.UserName,
                    FirstName = emp.FirstName,
                    LastName = emp.LastName,
                    Email = newUser.Email,
                    Created = DateTime.Now
                });
            }
            return Unauthorized();

        }

        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel loginUser)
        {
            var user = await _userManager.FindByNameAsync(loginUser.UserName);
            var employee = await nwContext.Employees.FindAsync(user.EmpId);
            if (user != null && employee != null && await _userManager.CheckPasswordAsync(user, loginUser.Password))
            {
                var getToken = GenerateJwtToken(user);
                var getRefreshToken = GenerateRefreshToken();
                
                user.LatestToken = await getToken;
                user.RefreshToken = getRefreshToken;
                await appContext.SaveChangesAsync();

                return Ok(new AuthenticateResponse() { FirstName = employee.FirstName, LastName = employee.LastName, UserName = user.UserName, Created = DateTime.Now, Email = user.Email, JwtToken = user.LatestToken.Value, RefreshToken = user.RefreshToken.Token, JwtExpires = user.LatestToken.Expires, RefreshExpires = user.RefreshToken.Expires });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new Response { StatusCode = "Error", Message = "" });
        }
        public async Task<Token> GenerateJwtToken(ApplicationUser user)
        {
            var employee = await nwContext.Employees.FindAsync(user.EmpId);
            var tokenHandler =  new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            var userRoles = await _userManager.GetRolesAsync(user);
            if(employee != null && userRoles != null)
            {
                var tokenId = Guid.NewGuid().ToString();

                var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, tokenId),
                new Claim(ClaimTypes.Country, employee.Country)
            };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    notBefore: DateTime.Now,
                    expires: DateTime.Now.AddMinutes(30),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
                    );;
                var generatedToken = tokenHandler.WriteToken(token);
                Token newToken = new Token();
                newToken.TokenId = tokenId;
                newToken.Value = generatedToken;
                newToken.Expires = token.ValidTo;
                await appContext.SaveChangesAsync();
                return newToken;
            }
            throw new ApplicationException("Invalid user request");

        }
        private (RefreshToken, ApplicationUser) GetRefreshToken(string refreshToken)
        {
            var user = appContext.Users.Include(u => u.RefreshToken).FirstOrDefault(u => u.RefreshToken.Token == refreshToken);
            var usersToken = user.RefreshToken;
            if(user == null || usersToken == null)
            {
                throw new ApplicationException("Invalid token");
            }
            
            if(usersToken.Token == refreshToken && usersToken.IsActive)
            {
                return (usersToken, user);
            }
            throw new ApplicationException("Invalid token");
        }
        [HttpPut]
        [Route("token/refreshtoken/{userRefreshToken}")]
        public async Task<AuthenticateResponse> RefreshToken(string userRefreshToken)
        {
            var (refreshToken, user) = GetRefreshToken(userRefreshToken);
            var newRefreshToken = GenerateRefreshToken();
            refreshToken.Revoked = DateTime.Now;
            refreshToken.ReplacedByToken = newRefreshToken.Token;
            user.RefreshToken = newRefreshToken;
            var jwtToken =  await GenerateJwtToken(user);
            user.LatestToken = jwtToken;
            
            var response = new AuthenticateResponse()
            {
                JwtToken = jwtToken.Value,
                RefreshToken = newRefreshToken.Token,
                JwtExpires = user.LatestToken.Expires,
                RefreshExpires = user.RefreshToken.Expires
                
            };
            await appContext.SaveChangesAsync();
            return response;
        }
        public RefreshToken GenerateRefreshToken()
        {
            return new RefreshToken
            {
                Token = RandomTokenString(),
                Expires = DateTime.Now.AddDays(10),
                Created = DateTime.Now,
                CreatedByIp = GetIpAdress()

            };
       
        }
        private string RandomTokenString()
        {
            using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            var randomBytes = new byte[40];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            return BitConverter.ToString(randomBytes).Replace("-", "");
        }
        private string GetIpAdress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            else
                return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }

    }
}
