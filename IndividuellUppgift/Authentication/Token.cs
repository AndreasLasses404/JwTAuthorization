using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IndividuellUppgift.Authentication
{
    public class Token
    {
        public string TokenId { get; set; }
        public string Value { get; set; }
        public DateTime Expires { get; set; }
        public bool Expired => DateTime.Now >= Expires;
    }
}
