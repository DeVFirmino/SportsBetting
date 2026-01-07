using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;
using SportsBetting.Tests.Common.Requests;

namespace WebApi.Test.Login.DoLogin;

public class DoLoginTest : SportsBettingClassFixture
{
    private readonly string method = "login";
    
    private readonly string _email = string.Empty;
    private readonly string _password;
    private readonly string _name;


    public DoLoginTest(CustomWebApplicationFactory factory) : base(factory) 
    {
         _email = factory.GetEmail();
       _password = factory.GetPassword();
       _name = factory.GetName();
       
    }

    [Fact]
    public async Task Sucess()
    {
        var request = new RequestLoginJson
        {
            Email = _email,
            Password = _password
        };

        var response = await DoPost(method, request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var responseBody = await response.Content.ReadAsStreamAsync();

        var responseData = await JsonDocument.ParseAsync(responseBody);

        responseData.RootElement.GetProperty("name").GetString().Should().Be(_name);
        responseData.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();

    }
    
    [Theory]
    [InlineData("en-US")]
    public async Task Error_Login_Invalid(string culture)
    {
        var request = RequestLoginJsonBuilder.Build();

        var response = await DoPost(method, request, culture);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await using var responseBody = await response.Content.ReadAsStreamAsync();

        var responseData = await JsonDocument.ParseAsync(responseBody);

        var errors = responseData.RootElement.GetProperty("errors").EnumerateArray();

        var expectedMessage = ResourcesMessagesException.ResourceManager.GetString("EMAIL_OR_PASSWORD_INVALID", new CultureInfo(culture));

        errors.Should().ContainSingle().And.Contain(error => error.GetString()!.Equals(expectedMessage));
    }
}
