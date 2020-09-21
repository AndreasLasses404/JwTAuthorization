using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NorthwindDb;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace IndividuellUppgift.Authentication
{
    public class ApplicationUser : IdentityUser
    {
        public int EmpId { get; set; }

        public Token LatestToken { get; set; }

        public RefreshToken RefreshToken { get; set; }
    }
}
