using AutoMapper.Configuration;
using IndividuellUppgift.Authentication;
using IndividuellUppgift.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using NorthwindDb;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
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
        [Route("getmyorders/{adminCeoEmp?}")]
        [Authorize]
        //Test
        //Tar en optional variabel av name
        public async Task<IActionResult> GetMyOrders(string adminCeoEmp = null)
        {
            //Hämtar användaren som skickar requesten
            var requestSender = _httpContext.HttpContext.User;
            var user = await _userManager.FindByNameAsync(requestSender.Identity.Name);
            var token = Request.Headers[HeaderNames.Authorization];
            if (!ValidateToken(user, token))
            {
                return Unauthorized();
            }

            //Kollar vilken roll användaren som skickar requesten har, samt om strängen är null.
            //Om strängen är null eller användaren endast är i rollen Employee så skickar den tillbaka de ordrar som användaren är kopplad till.
            if ((requestSender.IsInRole(UserRoles.Employee) && !requestSender.IsInRole(UserRoles.Admin) && !requestSender.IsInRole(UserRoles.CEO)) || 
                (requestSender.IsInRole(UserRoles.Admin) && adminCeoEmp == null) || (requestSender.IsInRole(UserRoles.CEO) && adminCeoEmp == null))
            {
                var employee = await _nwContext.Employees.Include(e => e.Orders).ThenInclude(c => c.Customer).FirstOrDefaultAsync(x => x.EmployeeId == user.EmpId);
                var ordersToGet = employee.Orders.ToList();
                var orders = GetOrdersList(ordersToGet);

                return Ok(orders);
            }
            //Om användaren är admin eller CEO och strängen inte är null, hämtar employee vars för- eller efternamn 
            //matchar strängen och skickar ut dennes orders
            if((requestSender.IsInRole(UserRoles.Admin) || requestSender.IsInRole(UserRoles.CEO)) && adminCeoEmp != null) 
            {
                var emp = await _nwContext.Employees.FirstOrDefaultAsync(e => e.FirstName == adminCeoEmp || e.LastName == adminCeoEmp);

                var ordersToGet = await _nwContext.Orders.Include(e => e.Employee).Include(c => c.Customer).Where(o => o.EmployeeId == emp.EmployeeId).ToListAsync();
                var orders = GetOrdersList(ordersToGet);
                return Ok(orders);
            }
            return Unauthorized();

        }

        [HttpGet]
        [Route("country/{countryName?}")]
        [Authorize]
        public IActionResult GetCountryOrders(string countryName = null)
        {
            var requestSender = _httpContext.HttpContext.User;
            var user = _dbContext.Users.Where(u => u.UserName == requestSender.Identity.Name).FirstOrDefault();
            var employee = _nwContext.Employees.Where(e => e.EmployeeId == user.EmpId).FirstOrDefault();
            var token = Request.Headers[HeaderNames.Authorization];
            if (!ValidateToken(user, token))
            {
                return Unauthorized();
            }
            if (user == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Message = "Invalid user", StatusCode = "Error" });
            }
            //Om countryname-strängen inte är null och användaren är admin eller ceo
            //görs en raw-sql query med parameter för att få ut ordrar baserat på shipcountry
            if ((requestSender.IsInRole(UserRoles.Admin) || requestSender.IsInRole(UserRoles.CEO)) && countryName != null)
            {
                using (SqlConnection sqlconn = new SqlConnection("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Northwind;Integrated Security=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"))
                {
                    var orders = new List<OrderModel>();
                    string query = $"SELECT o.OrderDate, o.Freight, o.ShipName, o.RequiredDate, o.ShippedDate, o.ShipCountry, c.CompanyName FROM Orders o " +
                    $"join Customers c on o.CustomerID = c.CustomerID " +
                    $"WHERE o.ShipCountry = @country ";
                    using (var comm = new SqlCommand(query, sqlconn))
                    {
                        comm.Parameters.Add("@country", SqlDbType.NVarChar);
                        comm.Parameters["@country"].Value = countryName;
                        sqlconn.Open();
                        using (var reader = comm.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var order = new OrderModel()
                                {
                                    ShipName = reader.GetString(reader.GetOrdinal("ShipName")),
                                    ShipCountry = reader.GetString(reader.GetOrdinal("ShipCountry")),
                                    CustomerName = reader.GetString(reader.GetOrdinal("CompanyName")),
                                    ShippedDate = reader.GetNullDateTime("ShippedDate"),
                                    OrderDate = reader.GetNullDateTime("OrderDate"),
                                    RequiredDate = reader.GetNullDateTime("RequiredDate"),
                                    Freight = reader.GetSqlMoney(reader.GetOrdinal("Freight")).ToString()
                                };
                                orders.Add(order);
                            }
                        }
                        return Ok(orders);
                    }
                }
            }
            //Om användaren är country manager görs en dbsökning baserat på den användarens country
            //och returnerar ordrar från det landet
            if (requestSender.IsInRole(UserRoles.CountryManager))
            {
                var otherEmployee = _nwContext.Employees.Include(e => e.Orders).ThenInclude(c => c.Customer).FirstOrDefault(u => u.EmployeeId == user.EmpId);
                var countryManagerOrders = _nwContext.Orders.Where(s => s.ShipCountry == employee.Country).ToList();
                var ordersResponse = GetOrdersList(countryManagerOrders);
                return Ok(ordersResponse);
            }
            return Unauthorized();
        }
        [HttpGet]
        [Route("getallorders")]
        [Authorize]
        public async Task<IActionResult> GetAllOrders()
        {
            var requestSender = _httpContext.HttpContext.User;
            var user = await _dbContext.Users.Include("LatestToken").Where(u => u.UserName == requestSender.Identity.Name).FirstOrDefaultAsync();
            var emp = await _nwContext.Employees.FindAsync(user.EmpId);
            var token = Request.Headers[HeaderNames.Authorization];
            if(!ValidateToken(user, token))
            {
                return Unauthorized();
            }
            //Om användaren är admin eller ceo hämtar den alla ordrar och returnerar dem i en lista av ordermodel
            if(requestSender.IsInRole(UserRoles.Admin) || requestSender.IsInRole(UserRoles.CEO))
            {
                var allOrders = await _nwContext.Orders.Include(c => c.Customer).ToListAsync();
                var orders = GetOrdersList(allOrders);
                return Ok(orders);
            }
            //Om användaren är countrymanager hämtar den ordrar från användarens land och sparar i en lista av ordermodel
            if (requestSender.IsInRole(UserRoles.CountryManager))
            {
                var allOrders = await _nwContext.Orders.Include(c => c.Customer).Where(sc => sc.ShipCountry == emp.Country).ToListAsync();
                var orders = GetOrdersList(allOrders);
                return Ok(orders);
            }
            return Unauthorized();
        }
        private List<OrderModel> GetOrdersList(List<Orders> orders)
        {
            var orderModelList = new List<OrderModel>();
            foreach (var order in orders)
            {
                var o = new OrderModel()
                {
                    CustomerName = order.Customer.CompanyName,
                    OrderDate = order.OrderDate,
                    ShippedDate = order.ShippedDate,
                    RequiredDate = order.RequiredDate,
                    ShipCountry = order.ShipCountry,
                    ShipName = order.ShipName,
                    Freight = order.Freight.ToString()
                };
                orderModelList.Add(o);
            }
            return orderModelList;
        }

        public static bool ValidateToken(ApplicationUser user, string token)
        {
            if ("Bearer " + user.LatestToken.Value == token)
            {
                return true;
            }
            return false;
        }

    }
    public static class ReaderExtenstions
    {
        //En extensionmetod som kollar om en datetime i är dbnull och om så är fallet, sätter null.
        //Denna metod körs vid den parametriserade sql queryn
        public static DateTime? GetNullDateTime(this SqlDataReader reader, string name)
        {
            var col = reader.GetOrdinal(name);
            return reader.IsDBNull(col) ?
                (DateTime?)null :
                (DateTime?)reader.GetDateTime(col);
        }
    }

}
