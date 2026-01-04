using Xunit;
using FluentAssertions;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using BurghExpress.Server.Models;
using BurghExpress.Server.Data;
using BurghExpress.Server.Permissions;
using BurghExpress.Server.DTO;
using BurghExpress.Tests.IntegrationTests.Helpers;
using BurghExpress.Server.Permissions;

namespace BurghExpress.Tests.IntegrationTests.Controllers;

public class UserRoleControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
{
  private readonly HttpClient _client;
  private readonly IServiceScope _scope;
  private readonly ApplicationDbContext _dbContext;
  private readonly UserManager<User> _userManager;

  public UserRoleControllerTests(CustomWebApplicationFactory<Program> factory)
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
    _scope.Dispose();
    _client.Dispose();
  }



  [Fact]
  public async Task CreateUserRole_Unauthenticated_ReturnsUnauthorized()
  {
    HttpResponseMessage response = await _client.PostAsync("/user/username/roles", new StringContent("", Encoding.UTF8, "application/json"));
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }


  [Fact]
  public async Task CreateUserRole_WithInsufficientPermissions_ReturnsForbidden()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext);

    HttpResponseMessage response = await client.PostAsync("/user/username/roles", new StringContent("", Encoding.UTF8, "application/json"));
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }



  [Fact]
  public async Task CreateUserRole_WithInvalidUserName_ReturnsNotFound()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.Create });

    StringContent requestData = new StringContent(@"{ ""Permission"": ""SomeName"" }", Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PostAsync("/user/username/roles", requestData);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    string stringResponse = await response.Content.ReadAsStringAsync();
    stringResponse.Should().Be("User not found.");
  }



  public static IEnumerable<object[]> InvalidUserRoleCreateData => 
    new(string field, object value, string error)[]
    {
      ("Role", 1233, ""),
      ("Role", "", "Role name is required."),
      ("Role", "InvalidRoleName", "Enter a valid role name."),
      ("Role", "SomeRole", "User is already in this role.")
    }.Select(item => 
        {
        var baseData = new Dictionary<string, object?>
        {
        ["Role"] = "TestRole"
        };
        baseData[item.field] = item.value;
        var json = JsonSerializer.Serialize(baseData);
        return new object[]{ json, item.field, item.error };
        });


  [Theory]
  [MemberData(nameof(InvalidUserRoleCreateData))]
  public async Task CreateUserRole_WithInvalidData_ReturnsBadRequest(string invalidData, string field, string error)
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserRoles.Create });

    Role testRole = new Role { Name = "SomePermission" };
    _dbContext.Permissions.Add(testPermission);
    await _dbContext.SaveChangesAsync();

    Status status = new Status { Name = "Active" };
    _dbContext.Statuses.Add(status);
    await _dbContext.SaveChangesAsync();

    User testUser = new User 
    {
      UserName = "testuserpermusername",
      FirstName = "FirstName",
      Email = "testuserpermemail@gmail.com",
      StatusId = status.Id,
      Status = status
    };
    await _userManager.CreateAsync(testUser, "Str0ngP@ssW0rd");

    UserRole testUserRole = new UserRole
    { 
      RoleId = testRole.Id,
      Role = testRole,
      UserId = testUser.Id,
      User = testUser
    };
    _dbContext.UserRoles.Add(testUserRole);
    await _dbContext.SaveChangesAsync();

    StringContent stringContent = new StringContent(invalidData, Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PostAsync($"/user/{testUser.UserName}/roles", stringContent);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    var stringResponse = await response.Content.ReadAsStringAsync();
    var jsonResponse = JsonDocument.Parse(stringResponse);

    jsonResponse.RootElement.GetProperty("title").GetString().Should().Be("One or more validation errors occurred.");
    jsonResponse.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();

    var errorField = field;
    var expectedError = error;
    if (string.IsNullOrEmpty(error))
    {
      errorField = $"$.{field}";
      expectedError = "could not be converted";
    }

    errors.TryGetProperty(errorField, out var fieldErrors).Should().BeTrue();

    var errorMessagesList = fieldErrors.EnumerateArray().Select(e => e.GetString()).ToList();
    errorMessagesList.Should().Contain(e => e.Contains(expectedError)); 
  }
  


  [Fact]
  public async Task CreateUserRole_ReturnsCreated()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserRoles.Create });

    Role testRole = new Role { Name = "SomePermission" };
    _dbContext.Permissions.Add(testPermission);
    await _dbContext.SaveChangesAsync();

    Status status = new Status { Name = "Active" };
    _dbContext.Statuses.Add(status);
    await _dbContext.SaveChangesAsync();

    User testUser = new User 
    {
      UserName = "testuserpermusername",
      FirstName = "FirstName",
      Email = "testuserpermemail@gmail.com",
      StatusId = status.Id,
      Status = status
    };
    await _userManager.CreateAsync(testUser, "Str0ngP@ssW0rd");

    StringContent requestData = new StringContent($@"{{ ""Permission"": ""{testRole.Name}"" }}", Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PostAsync($"/user/{testUser.UserName}/roles", requestData);

    response.StatusCode.Should().Be(HttpStatusCode.Created);

    string stringResponse = await response.Content.ReadAsStringAsync();
    var jsonResponse = JsonDocument.Parse(stringResponse);
    jsonResponse.RootElement.GetProperty("message").GetString().Should().Be("User added to role.");

    UserRoleResponseDTO createdUserRole = JsonSerializer.Deserialize<UserRoleResponseDTO>(jsonResponse.RootElement.GetProperty("userRole"), 
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    createdUserRole.User.FirstName.Should().Be(testUser.FirstName);
    createdUserRole.User.UserName.Should().Be(testUser.UserName);
    createdUserRole.Role.Name.Should().Be(testRole.Name);
    createdUserRole.Role.Description.Should().Be(testRole.Description);
  }
}
