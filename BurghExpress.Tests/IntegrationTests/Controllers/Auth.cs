using Xunit;
using FluentAssertions;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using BurghExpress.Server.Data;
using BurghExpress.Server.Models;

namespace BurghExpress.Tests.IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
{
  private readonly HttpClient _client;
  private readonly IServiceScope _scope;
  private readonly ApplicationDbContext _dbContext;
  private readonly UserManager<User> _userManager;


  public AuthControllerTests(CustomWebApplicationFactory<Program> factory)
  {
    _client = factory.CreateClient();
    _scope = factory.Services.CreateScope();
    _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    _dbContext.Database.EnsureDeleted();
    _dbContext.Database.EnsureCreated();
  }


  void IDisposable.Dispose()
  {
    _client.Dispose();
    _scope.Dispose();
  }



  public static IEnumerable<object[]> InvalidLoginJsonTestCases =>
    new(string field, object value)[]
    {
      ("Email", 1234),
      ("PassWord", 12334)
    }.Select(item => 
        {
        var baseData = new Dictionary<string, object?>
        {
        ["Email"] = "valid@email.com",
        ["PassWord"] = "ValidPassword"
        };
        baseData[item.field] = item.value;
        var json = JsonSerializer.Serialize(baseData);
        return new object[]{ json, item.field };
        });



  [Theory]
  [MemberData(nameof(InvalidLoginJsonTestCases))]
  public async Task Login_WithInvalidJson_ReturnsBadRequest(string invalidJson, string field)
  {
    var requestData = new StringContent(invalidJson, Encoding.UTF8, "application/json");
    var response = await _client.PostAsync("/auth/login", requestData);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    var stringResponse = await response.Content.ReadAsStringAsync();
    var jsonResponse = JsonDocument.Parse(stringResponse);

    jsonResponse.RootElement.GetProperty("title").GetString().Should().Be("One or more validation errors occurred.");
    jsonResponse.RootElement.GetProperty("errors").TryGetProperty($"$.{field}", out var fieldErrors).Should().BeTrue();
    var errorMessages = fieldErrors.EnumerateArray().Select(e => e.GetString()).ToList();
    errorMessages.Should().Contain(e => e.Contains("could not be converted to"));
  }



  public static IEnumerable<object[]> InvalidCredentialsTestCases =>
    new (string field, string value, string error)[]
    {
      ("Email", "nonexisting@email.com", "There is no account associated with this email address."),
      ("PassWord", "InvalidPassword", "Incorrect password.")
    }.Select(item => 
        {
        var baseData = new Dictionary<string, object?>
        {
        ["Email"] = "email@gmail.com",
        ["PassWord"] = "Str0ngP@ssw0rd"
        };
        baseData[item.field] = item.value;
        var json = JsonSerializer.Serialize(baseData);
        return new object[]{ json, item.field, item.error };
        });



  [Theory]
  [MemberData(nameof(InvalidCredentialsTestCases))]
  public async Task Login_WithInvalidCredentials_ReturnsBadRequest(string invalidData, string field, string error)
  {
    var testStatus = new Status { Name = "Active" };
    _dbContext.Statuses.Add(testStatus);
    await _dbContext.SaveChangesAsync();

    var testUser = new User
    {
      UserName = "someusername",
      FirstName = "TestUser",
      Email = "email@gmail.com",
      StatusId = testStatus.Id,
      Status = testStatus
    };
    var results = await _userManager.CreateAsync(testUser, "Str0ngP@ssW0rd");
    results.Succeeded.Should().BeTrue();

    var requestData = new StringContent(invalidData, Encoding.UTF8, "application/json");
    var response = await _client.PostAsync("/auth/login", requestData);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    var stringResponse = await response.Content.ReadAsStringAsync();
    var jsonResponse = JsonDocument.Parse(stringResponse);
    jsonResponse.RootElement.GetProperty("title").GetString().Should().Be("One or more validation errors occurred.");
    jsonResponse.RootElement.GetProperty("errors").TryGetProperty(field, out var fieldErrors).Should().BeTrue();
    var errorMessages = fieldErrors.EnumerateArray().Select(e => e.GetString()).ToList();
    errorMessages.Should().Contain(e => e == error);
  }



  [Fact]
  public async Task Login_WithValidCredentials_ReturnsOK()
  {
    var testStatus = new Status { Name = "Active" };
    _dbContext.Statuses.Add(testStatus);
    await _dbContext.SaveChangesAsync();

    var testUser = new User
    {
      UserName = "someusername",
      FirstName = "TestUser",
      Email = "email@gmail.com",
      StatusId = testStatus.Id,
      Status = testStatus
    };
    var results = await _userManager.CreateAsync(testUser, "Str0ngP@ssW0rd");
    results.Succeeded.Should().BeTrue();

    var validCredentials = @"{
      ""Email"": ""email@gmail.com"",
      ""PassWord"": ""Str0ngP@ssW0rd""
    }";
    var requestData = new StringContent(validCredentials, Encoding.UTF8, "application/json");
    var response = await _client.PostAsync("/auth/login", requestData);

    var stringResponse = await response.Content.ReadAsStringAsync();

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var jsonResponse = JsonDocument.Parse(stringResponse);
    jsonResponse.RootElement.GetProperty("token").Should().NotBeNull();
  }
}
