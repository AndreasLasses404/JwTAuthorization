using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NorthwindDb;

namespace IndividuellUppgift.Authentication
{
    public class ApplicationUser : IdentityUser
    {
        public Employees employee { get; set; }

    }
}
