using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IndividuellUppgift.Authentication
{
    public class Response
    {
        public int ResponseId { get; set; }
        public string StatusCode { get; set; }
        public string Message { get; set; }
        public string JwtToken { get; set; }
        public string RefreshToken { get; set; }

    }
}
