using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IndividuellUppgift.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace IndividuellUppgift.Models
{
    public class AuthenticateResponse
    {
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime Created { get; set; }
        public string JwtToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime RefreshExpires { get; set; }
        public DateTime JwtExpires { get; set; }
        public bool RefreshExpired => DateTime.Now >= RefreshExpires;
        public bool JwtExpired => DateTime.Now >= JwtExpires;
        

    }
}
