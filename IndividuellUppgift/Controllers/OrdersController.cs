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

            var requestee = _httpContext.HttpContext.User;
            var user = await _userManager.FindByNameAsync(requestee.Identity.Name);
            
            if ((requestee.IsInRole(UserRoles.Employee) && !requestee.IsInRole(UserRoles.Admin) && !requestee.IsInRole(UserRoles.CEO)) || 
                (requestee.IsInRole(UserRoles.Admin) && adminCeoEmp == null) || (requestee.IsInRole(UserRoles.CEO) && adminCeoEmp == null))
            {
                var employee = await _nwContext.Employees.Include(e => e.Orders).ThenInclude(c => c.Customer).FirstOrDefaultAsync(x => x.EmployeeId == user.EmpId);
                var orders = new List<OrderModel>();
                var query = employee.Orders.ToList();
                foreach (var q in query)
                {
                    var order = new OrderModel();
                    order.CustomerName = q.Customer.CompanyName;
                    order.OrderDate = q.OrderDate;
                    order.ShippedDate = q.ShippedDate;
                    order.RequiredDate = q.RequiredDate;
                    order.ShipCountry = q.ShipCountry;
                    order.ShipName = q.ShipName;
                    order.Freight = q.Freight.ToString();

                    orders.Add(order);
                }
                return Ok(orders);
            }
            if((requestee.IsInRole(UserRoles.Admin) || requestee.IsInRole(UserRoles.CEO)) && adminCeoEmp != null) 
            {
                var emp = await _nwContext.Employees.FirstOrDefaultAsync(e => e.FirstName == adminCeoEmp || e.LastName == adminCeoEmp);
                var orders = new List<OrderModel>();
                var query = await _nwContext.Orders.Include(c => c.Customer).Where(o => o.Employee == emp).ToListAsync();
                foreach (var q in query)
                {
                    var order = new OrderModel();
                    order.CustomerName = q.Customer.CompanyName;
                    order.OrderDate = q.OrderDate;
                    order.ShippedDate = q.ShippedDate;
                    order.RequiredDate = q.RequiredDate;
                    order.ShipCountry = q.ShipCountry;
                    order.ShipName = q.ShipName;
                    order.Freight = q.Freight.ToString();

                    orders.Add(order);
                }
                return Ok(orders);
            }
            return Unauthorized();

        }

        [HttpGet]
        [Route("country/{countryName?}")]
        [Authorize]
        public IActionResult GetCountryOrders(string countryName = null)
        {
            var requester = _httpContext.HttpContext.User;
            var user = _dbContext.Users.Where(u => u.UserName == requester.Identity.Name).FirstOrDefault();
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
                    if (requester.IsInRole(UserRoles.Admin) || requester.IsInRole(UserRoles.CEO))
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
                                    ShippedDate = reader.GetDateTime(reader.GetOrdinal("ShippedDate")),
                                    OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                                    RequiredDate = reader.GetDateTime(reader.GetOrdinal("RequiredDate")),
                                    Freight = reader.GetSqlMoney(reader.GetOrdinal("Freight")).ToString()

                                };
                                orders.Add(order);
                            }

                        }
                        sqlconn.Close();
                        return Ok(orders);
                    }
                }

                

                if (requester.IsInRole(UserRoles.CountryManager))
                {
                    var otherEmployee = _nwContext.Employees.Include(e => e.Orders).FirstOrDefault(u => u.EmployeeId == user.EmpId);
                    var countryManagerOrders = _nwContext.Orders.Where(s => s.ShipCountry == employee.Country);
                    sqlconn.Close();
                    return Ok(countryManagerOrders);
                }
                sqlconn.Close();
            }
            return Unauthorized();
        }
    }
}
