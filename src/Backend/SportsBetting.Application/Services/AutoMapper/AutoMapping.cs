using AutoMapper;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.Services.AutoMapper;

public sealed class AutoMapping : Profile
{
    public AutoMapping()
    {
        CreateMap<RegisterUserRequest, Domain.Entities.User>(MemberList.None)
            .ForMember(destination => destination.Password, options => options.Ignore());

        CreateMap<Domain.Entities.User, UserProfileResponse>();

        CreateMap<Domain.Entities.Bet, BetResponse>()
            .ForMember(
                destination => destination.Market,
                options => options.MapFrom(source => (Communication.Enums.BettingMarket)source.Market));
    }
}
