using AutoMapper.Configuration;
using IndividuellUppgift.Authentication;
using IndividuellUppgift.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
        public async Task<IActionResult> GetMyOrders(string adminCeoEmp = null)
        {

            var requestSender = _httpContext.HttpContext.User;
            var user = await _userManager.FindByNameAsync(requestSender.Identity.Name);
            
            if ((requestSender.IsInRole(UserRoles.Employee) && !requestSender.IsInRole(UserRoles.Admin) && !requestSender.IsInRole(UserRoles.CEO)) || 
                (requestSender.IsInRole(UserRoles.Admin) && adminCeoEmp == null) || (requestSender.IsInRole(UserRoles.CEO) && adminCeoEmp == null))
            {
                var employee = await _nwContext.Employees.Include(e => e.Orders).ThenInclude(c => c.Customer).FirstOrDefaultAsync(x => x.EmployeeId == user.EmpId);
                var ordersToGet = employee.Orders.ToList();
                var orders = GetOrdersList(ordersToGet);

                return Ok(orders);
            }
            if((requestSender.IsInRole(UserRoles.Admin) || requestSender.IsInRole(UserRoles.CEO)) && adminCeoEmp != null) 
            {
                var emp = await _nwContext.Employees.FirstOrDefaultAsync(e => e.FirstName == adminCeoEmp || e.LastName == adminCeoEmp);

                var ordersToGet = await _nwContext.Orders.Include(c => c.Customer).Where(o => o.Employee == emp).ToListAsync();
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
            
            if (user == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Message = "Invalid user", StatusCode = "Error" });
            }
            using (SqlConnection sqlconn = new SqlConnection("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Northwind;Integrated Security=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False"))
            {
                string query = $"SELECT o.OrderDate, o.Freight, o.ShipName, o.RequiredDate, o.ShippedDate, o.ShipCountry, c.CompanyName FROM Orders o " +
                    $"join Customers c on o.CustomerID = c.CustomerID " +
                    $"WHERE o.ShipCountry = @country ";
                using (var comm = new SqlCommand(query, sqlconn))
                {
                    comm.Parameters.Add("@country", SqlDbType.NVarChar);
                    comm.Parameters["@country"].Value = countryName;
                    var orders = new List<OrderModel>();
                    if (requestSender.IsInRole(UserRoles.Admin) || requestSender.IsInRole(UserRoles.CEO))
                    {
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
                        sqlconn.Close();
                        return Ok(orders);
                    }
                }
                if (requestSender.IsInRole(UserRoles.CountryManager))
                {
                    var otherEmployee = _nwContext.Employees.Include(e => e.Orders).FirstOrDefault(u => u.EmployeeId == user.EmpId);
                    var countryManagerOrders = _nwContext.Orders.Where(s => s.ShipCountry == employee.Country).ToList();
                    var orders = GetOrdersList(countryManagerOrders);
                    sqlconn.Close();
                    return Ok(orders);
                }
                sqlconn.Close();
            }
            return Unauthorized();
        }
        [HttpGet]
        [Route("getallorders")]
        [Authorize]
        public async Task<IActionResult> GetAllOrders()
        {
            var requestSender = _httpContext.HttpContext.User;
            var user = await _userManager.FindByNameAsync(requestSender.Identity.Name);
            var emp = await _nwContext.Employees.FindAsync(user.EmpId);
            if(requestSender.IsInRole(UserRoles.Admin) || requestSender.IsInRole(UserRoles.CEO))
            {
                var allOrders = await _nwContext.Orders.Include(c => c.Customer).ToListAsync();
                var orders = GetOrdersList(allOrders);
                return Ok(orders);
            }
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



    }
    public static class ReaderExtenstions
    {
        public static DateTime? GetNullDateTime(this SqlDataReader reader, string name)
        {
            var col = reader.GetOrdinal(name);
            return reader.IsDBNull(col) ?
                (DateTime?)null :
                (DateTime?)reader.GetDateTime(col);
        }
    }

}
