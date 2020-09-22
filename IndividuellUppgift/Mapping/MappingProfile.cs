using AutoMapper;
using IndividuellUppgift.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IndividuellUppgift.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<ApplicationUser, Response>().ReverseMap();
            CreateMap<Token, Response>().ReverseMap();
        }
    }
}
