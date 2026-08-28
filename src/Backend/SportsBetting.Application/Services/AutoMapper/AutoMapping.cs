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
          CreateMap<RegisterUserRequest, Domain.Entities.User>()
               .ForMember(dest => dest.Password,
                    opt => opt.Ignore())
               .ForMember(dest => dest.Active, opt => opt.MapFrom(_ => true));

          CreateMap<PlaceBetRequest, Domain.Entities.Bet>()
               .ForMember(dest => dest.Odds, opt => opt.Ignore())
               .ForMember(dest => dest.EventName, opt => opt.Ignore())
               .ForMember(dest => dest.BetType, opt => opt.Ignore())
               .ForMember(dest => dest.PotentialWinning, opt => opt.Ignore());


     }
     
     private void DomainToResponse()
     {
          CreateMap<Domain.Entities.User, UserProfileResponse>();
          
          CreateMap<Domain.Entities.Bet, BetResponse>();

     }
}
