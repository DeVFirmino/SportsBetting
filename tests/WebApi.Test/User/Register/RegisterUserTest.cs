using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using SportsBetting.Exceptions;
using SportsBetting.Tests.Common.Requests;

namespace WebApi.Test.User.Register;



public class RegisterUserTest : SportsBettingClassFixture
{
     private readonly string method = "user/register";
     
     public RegisterUserTest(CustomWebApplicationFactory factory) : base(factory) { }
     
     [Fact]
     public async Task ShouldRegisterUserWhenRequestIsValid()
     {
          var request = RegisterUserRequestBuilder.Build();

          var response = await DoPost(method, request);

          response.StatusCode.Should().Be(HttpStatusCode.Created);

          await using var responseBody = await response.Content.ReadAsStreamAsync();
          
          var responseData = await JsonDocument.ParseAsync(responseBody);
          
          responseData.RootElement.GetProperty("name").GetString().Should().Be(request.Name);
          responseData.RootElement.GetProperty("tokens").GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();


     }
     
     [Theory]
     [InlineData("en-US")]
     public async Task ShouldReturnBadRequestWhenNameIsEmpty(string culture)
     {
          var request = RegisterUserRequestBuilder.Build();
          request.Name = string.Empty;
          
          var response = await DoPost(method, request, culture);
          
          response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
          
          await using var responseBody = await response.Content.ReadAsStreamAsync();
          
          var responseData = await JsonDocument.ParseAsync(responseBody);

          var errors = responseData.RootElement.GetProperty("errors").EnumerateArray();
          
          var expectedMessage = ResourcesMessagesException.ResourceManager.GetString("NAME_EMPTY", new CultureInfo(culture));
          
          errors.Should().ContainSingle().And.Contain(error => error.GetString()!.Equals(expectedMessage));
     }
}
