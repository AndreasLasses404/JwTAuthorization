using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NorthwindDb
{
    public partial class Region
    {
        public Region()
        {
            Territories = new HashSet<Territories>();
        }
        [Key]
        public int RegionId { get; set; }
        public string RegionDescription { get; set; }

        public virtual ICollection<Territories> Territories { get; set; }
    }
}
