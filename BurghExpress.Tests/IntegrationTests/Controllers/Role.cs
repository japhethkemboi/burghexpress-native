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

public class RoleControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
{
  private readonly HttpClient _client;
  private readonly IServiceScope _scope;
  private readonly ApplicationDbContext _dbContext;
  private readonly UserManager<User> _userManager;

  public RoleControllerTests(CustomWebApplicationFactory<Program> factory)
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
  public async Task CreateRole_Unauthenticated_ReturnsUnauthorized()
  {
    HttpResponseMessage response = await _client.PostAsync("/roles", new StringContent("", Encoding.UTF8, "application/json"));
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }


  [Fact]
  public async Task CreateRole_WithInsufficientPermissions_ReturnsForbidden()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext);

    HttpResponseMessage response = await client.PostAsync("/roles", new StringContent("", Encoding.UTF8, "application/json"));
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }



  public static IEnumerable<object[]> InvalidRoleCreateData =>
    new (string field, object value, string error)[]
    {
      ("Name", 9878, ""),
      ("Name", "", "Role name is required."),
      ("Name", new string('a', 31), "Role name must be 30 characters or less."),
      ("Name", "Invalid Name", "Role name must contain only letters."),
      ("Name", "ExistingRole", "Role with this name already exists."),

      ("Description", new string('a', 501), "Role description must be 500 characters or less.")
    }.Select(entry =>
        {
        var baseData = new Dictionary<string, object?>
        {
        ["Name"] = "SomeRole",
        ["Description"] = "This is a test role.",
        };

        baseData[entry.field] = entry.value;

        string json = JsonSerializer.Serialize(baseData);
        return new object[] { json, entry.field, entry.error };
        });


  [Theory]
  [MemberData(nameof(InvalidRoleCreateData))]
  public async Task CreateRole_WithInvalidData_ReturnsBadRequest(string invalidData, string field, string? error)
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.Roles.Create });

    Role testRole = new Role { Name = "ExistingRole", Description = "Test role description." };
    _dbContext.Roles.Add(testRole);
    await _dbContext.SaveChangesAsync();

    StringContent requestData = new StringContent(invalidData, Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PostAsync("/roles", requestData);

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
  public async Task CreateRole_WithValidData_ReturnsCreated()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.Roles.Create });

    StringContent requestData = new StringContent(@"{
        ""Name"": ""SomeRoleName"",
        ""Description"": ""Role test description.""
        }", Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PostAsync("/roles", requestData);

    response.StatusCode.Should().Be(HttpStatusCode.Created);

    string stringResponse = await response.Content.ReadAsStringAsync();
    JsonDocument jsonResponse = JsonDocument.Parse(stringResponse);

    jsonResponse.RootElement.GetProperty("message").ToString().Should().Be("Role created.");

    PermissionResponseDTO createdPermission = JsonSerializer.Deserialize<PermissionResponseDTO>(jsonResponse.RootElement.GetProperty("role"),
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    createdPermission.Name.Should().Be("SomeRoleName");
    createdPermission.Description.Should().Be("Role test description.");
  }



  [Fact]
  public async Task PatchRole_Unauthenticated_ReturnsUnauthorized()
  {
    HttpResponseMessage response = await _client.PatchAsync("/roles/7", new StringContent("", Encoding.UTF8, "application/json"));
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }


  [Fact]
  public async Task PatchRole_WithInsufficientPermissions_ReturnsForbidden()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext);

    HttpResponseMessage response = await client.PatchAsync("/roles/7", new StringContent("", Encoding.UTF8, "application/json"));
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }



  [Fact]
  public async Task PatchRole_WithInvalidId_ReturnsNotFound()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.Roles.Patch });

    Role testRole = new Role { Name = "TestRole", Description = "Test role description." };
    _dbContext.Roles.Add(testRole);
    await _dbContext.SaveChangesAsync();

    StringContent requestData = new StringContent(@"{
        ""Name"": ""SomeRoleName"",
        ""Description"": ""Role test description.""
        }", Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PatchAsync("/roles/7", requestData);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);

    string stringResponse = await response.Content.ReadAsStringAsync();
    stringResponse.Should().Be("Role not found.");
  }


  public static IEnumerable<object[]> InvalidRolePatchData =>
    new (string field, object value, string error)[]
    {
      ("Name", 9878, ""),
      ("Name", new string('a', 31), "Role name must be 30 characters or less."),
      ("Name", "Invalid Name", "Role name must contain only letters."),
      ("Name", "ExistingRole", "Role with this name already exists."),

      ("Description", new string('a', 501), "Role description must be 500 characters or less.")
    }.Select(entry =>
        {
        var baseData = new Dictionary<string, object?>
        {
        ["Name"] = "SomeRole",
        ["Description"] = "This is a test role.",
        };

        baseData[entry.field] = entry.value;

        string json = JsonSerializer.Serialize(baseData);
        return new object[] { json, entry.field, entry.error };
        });


  [Theory]
  [MemberData(nameof(InvalidRolePatchData))]
  public async Task PatchRole_WithInvalidData_ReturnsBadRequest(string invalidData, string field, string error)
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.Roles.Patch });

    Role testRole = new Role { Name = "ExistingRole", Description = "Test role description." };
    _dbContext.Roles.Add(testRole);
    await _dbContext.SaveChangesAsync();

    StringContent requestData = new StringContent(invalidData, Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PatchAsync($"/roles/{testRole.Id}", requestData);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    string stringResponse = await response.Content.ReadAsStringAsync();
    JsonDocument jsonResponse = JsonDocument.Parse(stringResponse);

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

    List<string> errorMessagesList = fieldErrors.EnumerateArray().Select(e => e.GetString()).ToList();
    errorMessagesList.Should().Contain(e => e.Contains(expectedError)); 
  }



  [Fact]
  public async Task PatchRole_WithValidData_ReturnsCreated()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.Roles.Patch });

    Role testRole = new Role { Name = "TestRole", Description = "Test role description." };
    _dbContext.Roles.Add(testRole);
    await _dbContext.SaveChangesAsync();

    StringContent requestData = new StringContent(@"{
        ""Name"": ""SomeRoleName"",
        ""Description"": ""Role test description.""
        }", Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PatchAsync($"/roles/{testRole.Id}", requestData);

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    string stringResponse = await response.Content.ReadAsStringAsync();
    RoleResponseDTO patchedRole = JsonSerializer.Deserialize<RoleResponseDTO>(stringResponse,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    patchedRole.Name.Should().Be("SomeRoleName");
    patchedRole.Description.Should().Be("Role test description.");
  }



  [Fact]
  public async Task GetRole_Unauthenticated_ReturnsUnauthorized()
  {
    HttpResponseMessage response = await _client.GetAsync("/roles/7");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }


  [Fact]
  public async Task GetRole_WithInsufficientPermissions_ReturnsForbidden()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext);

    HttpResponseMessage response = await client.GetAsync("/roles/7");
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }



  [Fact]
  public async Task GetRole_WithInvalidId_ReturnsNotFound()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.Roles.View });

    HttpResponseMessage response = await client.GetAsync("/roles/7");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    string stringResponse = await response.Content.ReadAsStringAsync();
    stringResponse.Should().Be("Role not found.");
  }


  [Fact]
  public async Task GetRole_ReturnsOk()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.Roles.View });

    Role testRole = new Role { Name = "TestRole", Description = "Test role description." };
    _dbContext.Roles.Add(testRole);
    await _dbContext.SaveChangesAsync();

    HttpResponseMessage response = await client.GetAsync($"/roles/{testRole.Id}");

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    string stringResponse = await response.Content.ReadAsStringAsync();
    RoleResponseDTO foundRole = JsonSerializer.Deserialize<RoleResponseDTO>(stringResponse,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    foundRole.Name.Should().Be(testRole.Name);
    foundRole.Description.Should().Be(testRole.Description);
  }



  [Fact]
  public async Task GetRoles_Unauthenticated_ReturnsUnauthorized()
  {
    HttpResponseMessage response = await _client.GetAsync("/roles");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }


  [Fact]
  public async Task GetRoles_WithInsufficientPermissions_ReturnsForbidden()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext);

    HttpResponseMessage response = await client.GetAsync("/roles");
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }



  [Fact]
  public async Task GetRoles_ReturnsOk()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.Roles.View });

    Role testRole = new Role { Name = "TestRole", Description = "Test role description." };
    _dbContext.Roles.Add(testRole);
    await _dbContext.SaveChangesAsync();

    HttpResponseMessage response = await client.GetAsync("/roles");

    response.StatusCode.Should().Be(HttpStatusCode.OK);

    string stringResponse = await response.Content.ReadAsStringAsync();
    List<RoleResponseDTO> foundRoles = JsonSerializer.Deserialize<List<RoleResponseDTO>>(stringResponse,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    foundRoles[0].Name.Should().Be(testRole.Name);
    foundRoles[0].Description.Should().Be(testRole.Description);
  }



  [Fact]
  public async Task DeleteRole_Unauthenticated_ReturnsUnauthorized()
  {
    HttpResponseMessage response = await _client.DeleteAsync("/roles/7");
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }


  [Fact]
  public async Task DeleteRole_WithInsufficientPermissions_ReturnsForbidden()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext);

    HttpResponseMessage response = await client.DeleteAsync("/roles/7");
    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
  }



  [Fact]
  public async Task DeleteRole_WithInvalidId_ReturnsNotFound()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.Roles.Delete });

    HttpResponseMessage response = await client.DeleteAsync("/roles/7");

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    string stringResponse = await response.Content.ReadAsStringAsync();
    stringResponse.Should().Be("Role not found.");
  }



  [Fact]
  public async Task DeleteRole_ReturnsNoContent()
  {
    HttpClient client = await AuthHelper.GetAuthenticatedClientAsync(_client, _userManager, _dbContext, permissions: new[]{ ControllerPermissions.Roles.Delete });

    Role testRole = new Role { Name = "TestRole", Description = "Test role description." };
    _dbContext.Roles.Add(testRole);
    await _dbContext.SaveChangesAsync();

    HttpResponseMessage response = await client.DeleteAsync($"/roles/{testRole.Id}");

    response.StatusCode.Should().Be(HttpStatusCode.NoContent);
  }

}
