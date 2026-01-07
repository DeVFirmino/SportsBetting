using AutoMapper;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.Services.AutoMapper;

public class AutoMapping : Profile
{
     public AutoMapping()
     {
          RequestToDomain();
          DomainToResponse();
     }

     private void RequestToDomain()
     {
          CreateMap<RequestRegisterUserJson, Domain.Entities.User>()
               .ForMember(dest => dest.Password,
                    opt => opt.Ignore())
               .ForMember(dest => dest.Active, opt => opt.MapFrom(_ => true));
     }
     
     private void DomainToResponse()
     {
          CreateMap<Domain.Entities.User, ResponseUserProfileJson>();

     }
}