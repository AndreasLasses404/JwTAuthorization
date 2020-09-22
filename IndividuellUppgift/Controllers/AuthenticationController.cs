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

                return Ok(new Response {StatusCode ="Success", Message ="User logged in. Eat his kebab", JwtToken = user.LatestToken.Value, RefreshToken = user.RefreshToken.Token});
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
                    expires: DateTime.Now.AddMinutes(30),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
                    );
                var generatedToken = tokenHandler.WriteToken(token);
                Token newToken = new Token();
                newToken.TokenId = tokenId;
                newToken.Value = generatedToken;
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
        public async Task<Response> RefreshToken(string userRefreshToken)
        {
            var (refreshToken, user) = GetRefreshToken(userRefreshToken);

            var newRefreshToken = GenerateRefreshToken();
            refreshToken.Revoked = DateTime.Now;
            refreshToken.ReplacedByToken = newRefreshToken.Token;
            user.RefreshToken = newRefreshToken;

            var jwtToken =  await GenerateJwtToken(user);

            var response = new Response()
            {
                JwtToken = jwtToken.Value,
                RefreshToken = newRefreshToken.Token
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
