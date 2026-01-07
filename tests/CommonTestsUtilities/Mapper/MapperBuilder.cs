using AutoMapper;
using SportsBetting.Application.Services.AutoMapper;

namespace SportsBetting.Tests.Common.Mapper;

public class MapperBuilder
{
    public static IMapper Build()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new AutoMapping());
        });

        return new AutoMapper.Mapper(config);
    }
}
