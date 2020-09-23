using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IndividuellUppgift.Models
{
    public class OrderModel
    {
        public string CustomerName { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? RequiredDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public string ShipCountry { get; set; }
        public string ShipName { get; set; }
        public string Freight { get; set; }

    }
}
