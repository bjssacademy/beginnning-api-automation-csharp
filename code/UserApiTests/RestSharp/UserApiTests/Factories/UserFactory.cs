using UserApiTests.Models;

namespace UserApiTests.Factories
{
    public class UserFactory
    {
        public static User CreateUser()
        {
            return new User
            {
                Name = "John Doe_" + Guid.NewGuid(),
                Password = "SecurePass123"
            };
        }
    }
}
