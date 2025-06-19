namespace BurghExpress.Tests.UnitTests.DTO;

public class UserCreateDTOTests
{
  public static IEnumerable<object[]> InvalidUserCreateData() => 
    new (string field, object value, string error)[]
    {

    }
  .Select(entry => 
      {
      var baseData = new Dictionary<string, object?>
      {
      UserName = "someusername",
      FirstName = "söménamé",
      LastName = "somelästname",
      PhoneNumber = "1234567890",
      Email = "email@gmail.com",
      PassWord = "S0m3strongPass"
      };
      });
}
