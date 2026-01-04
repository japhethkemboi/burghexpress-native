using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using BurghExpress.Server.Data;
using BurghExpress.Server.Models;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
  private SqliteConnection? _connection;

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureServices(services =>
        {
        var descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
        if(descriptor == null) services.Remove(descriptor);

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        services.AddDataProtection();

        services.AddIdentityCore<User>()
        .AddRoles<Role>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        using var scope = services.BuildServiceProvider().CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        });

    builder.UseEnvironment("Development");
  }

  protected override void Dispose(bool disposing)
  {
    base.Dispose(disposing);
    _connection?.Close();
    _connection?.Dispose();
  }
}
