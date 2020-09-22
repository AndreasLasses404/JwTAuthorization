using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IndividuellUppgift.Models
{
    public class OrderModel
    {
        public string CustomerName { get; set; }
        public string OrderDate { get; set; }
        public string RequiredDate { get; set; }
        public string ShippedDate { get; set; }
        public string ShipCountry { get; set; }
        public string ShipName { get; set; }
        public string Freight { get; set; }

    }
}
