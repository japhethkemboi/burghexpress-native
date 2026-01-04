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

public class UserPermissionControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
{
  private readonly HttpClient _client;
  private readonly IServiceScope _scope;
  private readonly ApplicationDbContext _dbContext;
  private readonly UserManager<User> _userManager;

  public UserPermissionControllerTests(CustomWebApplicationFactory<Program> factory)
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
  public async Task CreateUserPermission_Unauthenticated_ReturnsUnauthorized()
  {
    HttpResponseMessage response = await _client.PostAsync("/user/username/permissions", new StringContent("", Encoding.UTF8, "application/json"));
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }


  [Fact]
  public async Task CreateUserPermission_WithInsufficientPermissions_ReturnsForbidden()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext);

    HttpResponseMessage response = await client.PostAsync("/user/username/permissions", new StringContent("", Encoding.UTF8, "application/json"));
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }



  [Fact]
  public async Task CreateUserPermission_WithInvalidUserName_ReturnsNotFound()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.Create });

    StringContent requestData = new StringContent(@"{ ""Permission"": ""SomeName"" }", Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PostAsync("/user/username/permissions", requestData);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    string stringResponse = await response.Content.ReadAsStringAsync();
    stringResponse.Should().Be("User not found.");
  }



  public static IEnumerable<object[]> InvalidUserPermissionCreateData => 
    new(string field, object value, string error)[]
    {
      ("Permission", 1233, ""),
      ("Permission", "", "Permission name is required."),
      ("Permission", "InvalidPermissionName", "Enter a valid permission name."),
      ("Permission", "SomePermission", "User already has this permission.")
    }.Select(item => 
        {
        var baseData = new Dictionary<string, object?>
        {
        ["Permission"] = ControllerPermissions.UserPermissions.Create
        };
        baseData[item.field] = item.value;
        var json = JsonSerializer.Serialize(baseData);
        return new object[]{ json, item.field, item.error };
        });


  [Theory]
  [MemberData(nameof(InvalidUserPermissionCreateData))]
  public async Task CreateUserPermission_WithInvalidData_ReturnsBadRequest(string invalidData, string field, string error)
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.Create });

    Permission testPermission = new Permission { Name = "SomePermission" };
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

    UserPermission testUserPermission = new UserPermission 
    { 
      PermissionId = testPermission.Id,
      Permission = testPermission,
      UserId = testUser.Id,
      User = testUser
    };
    _dbContext.UserPermissions.Add(testUserPermission);
    await _dbContext.SaveChangesAsync();

    StringContent stringContent = new StringContent(invalidData, Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PostAsync($"/user/{testUser.UserName}/permissions", stringContent);

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
  public async Task CreateUserPermission_ReturnsCreated()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.Create });

    Permission testPermission = new Permission { Name = "SomePermission", Description = "Test desc" };
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

    StringContent requestData = new StringContent($@"{{ ""Permission"": ""{testPermission.Name}"" }}", Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PostAsync($"/user/{testUser.UserName}/permissions", requestData);

    response.StatusCode.Should().Be(HttpStatusCode.Created);

    string stringResponse = await response.Content.ReadAsStringAsync();
    var jsonResponse = JsonDocument.Parse(stringResponse);
    jsonResponse.RootElement.GetProperty("message").GetString().Should().Be("Permission granted.");

    UserPermissionResponseDTO createdUserPermission = JsonSerializer.Deserialize<UserPermissionResponseDTO>(jsonResponse.RootElement.GetProperty("userPermission"), 
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    createdUserPermission.User.FirstName.Should().Be(testUser.FirstName);
    createdUserPermission.User.UserName.Should().Be(testUser.UserName);
    createdUserPermission.Permission.Name.Should().Be(testPermission.Name);
    createdUserPermission.Permission.Description.Should().Be(testPermission.Description);
  }



  [Fact]
  public async Task PatchUserPermission_Unauthenticated_ReturnsUnauthorized()
  {
    HttpResponseMessage response = await _client.PatchAsync("/user/username/permissions/7", new StringContent("", Encoding.UTF8, "application/json"));
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }


  [Fact]
  public async Task PatchUserPermission_WithInsufficientPermissions_ReturnsForbidden()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext);

    HttpResponseMessage response = await client.PatchAsync("/user/username/permissions/7", new StringContent("", Encoding.UTF8, "application/json"));
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }



  [Fact]
  public async Task PatchUserPermission_WithInvalidUserName_ReturnsNotFound()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.Patch });

    StringContent requestData = new StringContent(@"{ ""Permission"": ""SomeName"" }", Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PatchAsync("/user/username/permissions/7", requestData);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    string stringResponse = await response.Content.ReadAsStringAsync();
    stringResponse.Should().Be("User not found.");
  }



  [Fact]
  public async Task PatchUserPermission_WithInvalidUserPermissionId_ReturnsNotFound()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.Patch });

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

    StringContent requestData = new StringContent(@"{ ""Permission"": ""SomeName"" }", Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PatchAsync($"/user/{testUser.UserName}/permissions/7", requestData);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    string stringResponse = await response.Content.ReadAsStringAsync();
    stringResponse.Should().Be("User permission not found.");
  }



  [Theory]
  [MemberData(nameof(InvalidUserPermissionCreateData))]
  public async Task PatchUserPermission_WithInvalidData_ReturnsBadRequest(string invalidData, string field, string error)
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.Patch });

    Permission testPermission = new Permission { Name = "SomePermission" };
    _dbContext.Permissions.Add(testPermission);

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

    UserPermission testUserPermission = new UserPermission 
    { 
      PermissionId = testPermission.Id,
      Permission = testPermission,
      UserId = testUser.Id,
      User = testUser
    };
    _dbContext.UserPermissions.Add(testUserPermission);
    await _dbContext.SaveChangesAsync();

    StringContent stringContent = new StringContent(invalidData, Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PatchAsync($"/user/{testUser.UserName}/permissions/{testUserPermission.Id}", stringContent);

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
  public async Task PatchUserPermission_ReturnsOk()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.Patch });

    Permission testPermission = new Permission { Name = "SomePermission" };
    _dbContext.Permissions.Add(testPermission);

    Permission testPermission2 = new Permission { Name = "SomePermission2", Description = "Test desc" };
    _dbContext.Permissions.Add(testPermission2);

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

    UserPermission testUserPermission = new UserPermission 
    { 
      PermissionId = testPermission.Id,
      Permission = testPermission,
      UserId = testUser.Id,
      User = testUser
    };
    _dbContext.UserPermissions.Add(testUserPermission);
    await _dbContext.SaveChangesAsync();

    StringContent requestData = new StringContent($@"{{ ""Permission"": ""{testPermission2.Name}"" }}", Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PatchAsync($"/user/{testUser.UserName}/permissions/{testUserPermission.Id}", requestData);

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    string stringResponse = await response.Content.ReadAsStringAsync();
    var jsonResponse = JsonDocument.Parse(stringResponse);
    jsonResponse.RootElement.GetProperty("message").GetString().Should().Be("Permission updated.");

    UserPermissionResponseDTO patchedUserPermission = JsonSerializer.Deserialize<UserPermissionResponseDTO>(jsonResponse.RootElement.GetProperty("userPermission"), 
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    patchedUserPermission.User.FirstName.Should().Be(testUser.FirstName);
    patchedUserPermission.User.UserName.Should().Be(testUser.UserName);
    patchedUserPermission.Permission.Name.Should().Be(testPermission2.Name);
    patchedUserPermission.Permission.Description.Should().Be(testPermission2.Description);
  }



  [Fact]
  public async Task GetUserPermission_Unauthenticated_ReturnsUnauthorized()
  {
    HttpResponseMessage response = await _client.GetAsync("/user/username/permissions/7");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }


  [Fact]
  public async Task GetUserPermission_WithInsufficientPermissions_ReturnsForbidden()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext);

    HttpResponseMessage response = await client.GetAsync("/user/username/permissions/7");
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }



  [Fact]
  public async Task GetUserPermission_WithInvalidUserName_ReturnsNotFound()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.View });

    HttpResponseMessage response = await client.GetAsync("/user/username/permissions/7");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    string stringResponse = await response.Content.ReadAsStringAsync();
    stringResponse.Should().Be("User not found.");
  }



  [Fact]
  public async Task GetUserPermission_WithInvalidUserPermissionId_ReturnsNotFound()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.View });

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

    HttpResponseMessage response = await client.GetAsync($"/user/{testUser.UserName}/permissions/7");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    string stringResponse = await response.Content.ReadAsStringAsync();
    stringResponse.Should().Be("User permission not found.");
  }
  


  [Fact]
  public async Task GetUserPermission_ReturnsOk()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.View });

    Permission testPermission = new Permission { Name = "SomePermission" };
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

    UserPermission testUserPermission = new UserPermission 
    { 
      PermissionId = testPermission.Id,
      Permission = testPermission,
      UserId = testUser.Id,
      User = testUser
    };
    _dbContext.UserPermissions.Add(testUserPermission);
    await _dbContext.SaveChangesAsync();

    HttpResponseMessage response = await client.GetAsync($"/user/{testUser.UserName}/permissions/{testUserPermission.Id}");

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    string stringResponse = await response.Content.ReadAsStringAsync();

    UserPermissionResponseDTO fetchedUserPermission = JsonSerializer.Deserialize<UserPermissionResponseDTO>(stringResponse, 
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    fetchedUserPermission.User.FirstName.Should().Be(testUser.FirstName);
    fetchedUserPermission.User.UserName.Should().Be(testUser.UserName);
    fetchedUserPermission.Permission.Name.Should().Be(testPermission.Name);
  }



  [Fact]
  public async Task GetUserPermissions_Unauthenticated_ReturnsUnauthorized()
  {
    HttpResponseMessage response = await _client.GetAsync("/user/username/permissions");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }


  [Fact]
  public async Task GetUserPermissions_WithInsufficientPermissions_ReturnsForbidden()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext);

    HttpResponseMessage response = await client.GetAsync("/user/username/permissions");
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }



  [Fact]
  public async Task GetUserPermissions_WithInvalidUserName_ReturnsNotFound()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.View });

    HttpResponseMessage response = await client.GetAsync("/user/username/permissions");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    string stringResponse = await response.Content.ReadAsStringAsync();
    stringResponse.Should().Be("User not found.");
  }
  


  [Fact]
  public async Task GetUserPermissions_ReturnsOk()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.View });

    Permission testPermission = new Permission { Name = "SomePermission" };
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

    UserPermission testUserPermission = new UserPermission 
    { 
      PermissionId = testPermission.Id,
      Permission = testPermission,
      UserId = testUser.Id,
      User = testUser
    };
    _dbContext.UserPermissions.Add(testUserPermission);
    await _dbContext.SaveChangesAsync();

    HttpResponseMessage response = await client.GetAsync($"/user/{testUser.UserName}/permissions");

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    string stringResponse = await response.Content.ReadAsStringAsync();

    List<UserPermissionResponseDTO> fetchedUserPermissions = JsonSerializer.Deserialize<List<UserPermissionResponseDTO>>(stringResponse, 
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    fetchedUserPermissions[0].User.FirstName.Should().Be(testUser.FirstName);
    fetchedUserPermissions[0].User.UserName.Should().Be(testUser.UserName);
    fetchedUserPermissions[0].Permission.Name.Should().Be(testPermission.Name);
  }



  [Fact]
  public async Task DeleteUserPermission_Unauthenticated_ReturnsUnauthorized()
  {
    HttpResponseMessage response = await _client.DeleteAsync("/user/username/permissions/7");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }


  [Fact]
  public async Task DeleteUserPermission_WithInsufficientPermissions_ReturnsForbidden()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext);

    HttpResponseMessage response = await client.DeleteAsync("/user/username/permissions/7");
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }



  [Fact]
  public async Task DeleteUserPermission_WithInvalidUserName_ReturnsNotFound()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.Delete });

    HttpResponseMessage response = await client.DeleteAsync("/user/username/permissions/7");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    string stringResponse = await response.Content.ReadAsStringAsync();
    stringResponse.Should().Be("User not found.");
  }



  [Fact]
  public async Task DeleteUserPermission_WithInvalidUserPermissionId_ReturnsNotFound()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.Delete });

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

    HttpResponseMessage response = await client.DeleteAsync($"/user/{testUser.UserName}/permissions/7");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    string stringResponse = await response.Content.ReadAsStringAsync();
    stringResponse.Should().Be("User permission not found.");
  }
  


  [Fact]
  public async Task DeleteUserPermission_ReturnsNoContent()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.UserPermissions.Delete });

    Permission testPermission = new Permission { Name = "SomePermission" };
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

    UserPermission testUserPermission = new UserPermission 
    { 
      PermissionId = testPermission.Id,
      Permission = testPermission,
      UserId = testUser.Id,
      User = testUser
    };
    _dbContext.UserPermissions.Add(testUserPermission);
    await _dbContext.SaveChangesAsync();

    HttpResponseMessage response = await client.DeleteAsync($"/user/{testUser.UserName}/permissions/{testUserPermission.Id}");

    response.StatusCode.Should().Be(HttpStatusCode.NoContent);
  }
}
