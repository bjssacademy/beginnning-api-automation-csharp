# Patterns & Antipatterns

Previously we used `[SetUp]` for user creation, to stop us repeating ourselves in each test. And it seemed like a really good idea. However, you may have noticed that refactoring your `CreateUser_ShouldReturnUser` test now doesn't do a lot. It merely asserts, because creation is done elsewhere.

The concern here is that when using **[SetUp]** for user creation, the logic of the test itself can become diluted, which can make the test harder to understand and maintain. The **Arrange** and **Act** phases, which are crucial to the test's clarity, are no longer contained within the test method. 

## **What Are The Problems That Point To Antipattern?**

1. **Loss of Clarity** 

    By moving the user creation logic into **[SetUp]**, the test no longer explicitly shows what it is testing. The essence of the **Arrange** and **Act** phases is diluted, as the creation happens automatically before the test runs. The reader or developer might need to inspect the **[SetUp]** method to fully understand what data is being prepared for the test, which makes the test less readable.
  
2. **Test Independence** 

    The **[SetUp]** and **[TearDown]** methods introduce hidden dependencies between the test and the setup process. If the **[SetUp]** fails (e.g., the user creation fails for some reason), the test cannot be executed correctly. This ties the test to the setup process more tightly than is ideal.

3. **Test Overhead** 

    The **[SetUp]** logic runs before each test, which might be unnecessary overhead if only a small number of tests require user creation. Tests that don't need user creation will still trigger the **[SetUp]** method, which can lead to additional API calls, slowing down the overall test suite. 

## **Alternatives to Avoid This**

1. **Create Users Within the Test**
   
    If creating a user is central to what you're testing, then it's better to leave the user creation inside the test. This keeps the **Arrange** and **Act** phases contained in the test method itself, making it clear and focused - it also helps to make the test independent of others.
   
   **However**, we end up duplicating a lot of code - and if we had the `[Setup]` method then we'd be creating extra users we don't need, as well as adding extraneous calls to the database. Not great.

2. **Separate Setup Logic from Tests (Factory or Builder Pattern)**
   
    Instead of using **[SetUp]** to create the user, we could create a separate service that builds the test data we need. This way, we avoid putting the setup logic directly into the test and can still maintain control over what each test is doing.

   Let's look at what someone might first do when they hear about the factory pattern as a *creational* pattern -
   
   ```csharp
   public class UserFactory
   {
       public static async Task<User> CreateUserAsync(HttpClient client)
       {
           var newUser = new User
           {
               Name = "John Doe99_" + Guid.NewGuid(),
               Password = "SecurePass123"
           };
           var createResponse = await client.PostAsJsonAsync("", newUser);
           createResponse.EnsureSuccessStatusCode();
           return await createResponse.Content.ReadFromJsonAsync<User>();
       }
   }
   ```

   The test can then call this factory method to create the user:
   
   ```csharp
   [Test]
   public async Task CreateUser_ShouldReturnUser()
   {
       // Arrange
       var createdUser = await UserFactory.CreateUserAsync(_client);

       // Act
       var getResponse = await _client.GetAsync(createdUser.Id.ToString());
       getResponse.EnsureSuccessStatusCode();
       var user = await getResponse.Content.ReadFromJsonAsync<User>();

       // Assert
       Assert.NotNull(user);
       Assert.AreEqual(createdUser.Id, user.Id);
       Assert.That(createdUser.Name, Is.EqualTo(newUser.Name));

       // Cleanup
       var deleteResponse = await _client.DeleteAsync(createdUser.Id.ToString());
       deleteResponse.EnsureSuccessStatusCode();
   }
   ```
## But wait...aren't we just putting creation somewhere else and creating the same problem?

Well spotted. A proper factory should focus on creating the **object**, not handling API interactions. The responsibility of making API calls should be handled separately, (likely by a repository or service layer, but we'll get to that later).  

### **Correct Approach Using a Factory Pattern**  

A **factory** should create instances of the `User` model, while the **test itself** is responsible for sending the user to the API.  

Follow along now as we:

1. Create a `Userfactory` class
2. Use the factory in the test

#### **Step 1: Create a User Factory**  
The factory simply **creates** a `User` instance with a unique name but does not persist it.  

1. Create a new folder, `Factories` in the solution.
2. Add a new class, `UserFactory.cs` and replace the generated code with the code below:

```csharp
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
```

---

#### **Step 2: Use the Factory in the Test**  
Now, the **test remains responsible for making API requests**, but it **delegates object creation** to the factory.  

```csharp

public class UserApiTests
{
private HttpClient _client;

    [SetUp]
    public async Task SetUp()
    {
        _client = new HttpClient { BaseAddress = new Uri("https://localhost:7098/api/Users/") };  //replace URL with whatever your URL is
    }

    [Test]
    public async Task CreateUser_ShouldReturnUser()
    {
        var newUser = UserFactory.CreateUser();

        var response = await _client.PostAsJsonAsync("", newUser);
        response.EnsureSuccessStatusCode();

        var createdUser = await response.Content.ReadFromJsonAsync<User>();

        Assert.NotNull(createdUser);
        Assert.Greater(createdUser.Id, 0);
        Assert.That(createdUser.Name, Is.EqualTo(newUser.Name));
        Assert.That(createdUser.Password, Is.EqualTo(newUser.Password));

        var deleteResponse = await _client.DeleteAsync(createdUser.Id.ToString());
        deleteResponse.EnsureSuccessStatusCode();

    }

    [Test]
    public async Task GetUser_ShouldReturnUser()
    {

        var newUser = UserFactory.CreateUser();

        var response = await _client.PostAsJsonAsync("", newUser);
        response.EnsureSuccessStatusCode();

        var createdUser = await response.Content.ReadFromJsonAsync<User>();

        response = await _client.GetAsync(createdUser.Id.ToString());
        response.EnsureSuccessStatusCode();

        var user = await response.Content.ReadFromJsonAsync<User>();

        Assert.NotNull(user);
        Assert.That(user.Id, Is.EqualTo(createdUser.Id));
        Assert.That(user.Name, Is.EqualTo(createdUser.Name));
        Assert.That(user.Password, Is.EqualTo(createdUser.Password));

        var deleteResponse = await _client.DeleteAsync(createdUser.Id.ToString());
        deleteResponse.EnsureSuccessStatusCode();
    }

    //... other tests

    [TearDown]
    public async Task TearDown()
    {
        _client.Dispose();
    }


}
```

---

### **Why Is This Better?**
For one, the Factory creates the object, not the API request – keeping responsibilities clean.  Secondly, the test remains responsible for API interactions, making it clearer.  

Finally, you can adjust object creation logic without touching the test structure.  

### **Why Is This Problematic?**

We've sort of gone backwards. We've now got a lot of duplicated code we will have to call in every test to create and delete the user. Whilst we want the test to have control over its own data, we do want to avoid this sort of repetition.

## Introducing The Service Layer

A **service layer** is a layer in your application that sits between your tests (or other consumers) and an API. Instead of writing API calls directly in every test, we **encapsulate** this logic in a separate `UserService` class.  

By introducing a service layer, our tests **only care about the outcome** (i.e., user creation, retrieval, deletion) instead of manually handling HTTP requests in every test.  

### **What is Encapsulation?**  
**Encapsulation** is the idea of **hiding complex logic behind a simple interface**. Instead of exposing raw API calls in every test, we **bundle them inside a service class** (`UserService`).  

We often use the example of a car: 
- You don’t need to know how the engine works to drive (i.e., you don’t need to write HTTP calls in every test).  
- You just press the accelerator and go (i.e., call `userService.CreateUserAsync(user)`).  
- The engine (API logic) is **hidden behind an abstraction**, making it easier to use.  

---

## Add A Service Layer

1. Create a new Folder `Services` in the solution
2. Add a new file `UserService.cs` in the `Services` folder
3. Replace the generated code:

```cs
using System.Net.Http.Json;
using UserApiTests.Models;

namespace UserApiTests.Services
{
    public class UserService
    {
        private readonly HttpClient _client;

        public UserService(HttpClient client)
        {
            _client = client;
        }

        public async Task<User> CreateUserAsync(User user)
        {
            var response = await _client.PostAsJsonAsync("api/Users", user);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<User>();
        }

        public async Task<User> GetUserByIdAsync(long userId)
        {
            var response = await _client.GetAsync($"api/Users/{userId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<User>();
        }

        public async Task DeleteUserAsync(long userId)
        {
            var response = await _client.DeleteAsync($"api/Users/{userId}");
            response.EnsureSuccessStatusCode();
        }
    }

}
```

### **The `UserService` Class**  

#### **1. Constructor: `UserService(HttpClient client)`**  
The constructor takes an `HttpClient` as a dependency and assigns it to a private field `_client`.  
- This allows the service to make HTTP requests to the API.  
- Using **dependency injection** ensures `HttpClient` is properly managed and reused.  

---

#### **2️. Method: `CreateUserAsync(User user)`**  
This method **sends a POST request** to `api/Users` to create a new user.  
- It serialises the `user` object into JSON and sends it to the API.  
- It ensures the request was successful with `EnsureSuccessStatusCode()`.  
- Finally, it **deserialises** the response back into a `User` object and returns it.  

---

#### **3️. Method: `GetUserByIdAsync(long userId)`**  
This method **retrieves a user** by their `id` using a **GET request**.  
- It requests `api/Users/{id}` from the API.  
- If the response is successful, it **parses the JSON into a `User` object** and returns it.  

---

#### **4️. Method: `DeleteUserAsync(long userId)`**  
This method **sends a DELETE request** to remove a user by ID.  
- It calls `api/Users/{id}` using the DELETE method.  
- If the request is **not** successful, `EnsureSuccessStatusCode()` will throw an exception, preventing silent failures.  
- Unlike the other methods, it **does not return a value** because a successful deletion doesn’t need a response body.  

---

## Use The Service Layer

Update your code, and a couple of tests like so:

> :exclamation: Please note the change in the URL to be the root domain, not complete path to the resource.

```cs
[TestFixture]
public class UserApiTests
{
    private HttpClient _client;
    private UserService _userService;

    [SetUp]
    public async Task SetUp()
    {
        _client = new HttpClient { BaseAddress = new Uri("https://localhost:7098/") };  //replace URL with whatever your URL is
        _userService = new UserService(_client);
    }

    [Test]
    public async Task CreateUser_ShouldReturnUser()
    {
        var newUser = UserFactory.CreateUser();
        var createdUser = await _userService.CreateUserAsync(newUser);

        Assert.NotNull(createdUser);
        Assert.Greater(createdUser.Id, 0);
        Assert.That(createdUser.Name, Is.EqualTo(newUser.Name));
        Assert.That(createdUser.Password, Is.EqualTo(newUser.Password));

        await _userService.DeleteUserAsync(createdUser.Id);

    }

    [Test]
    public async Task GetUser_ShouldReturnUser()
    {
        var newUser = UserFactory.CreateUser();
        var createdUser = await _userService.CreateUserAsync(newUser);

        var user = await _userService.GetUserByIdAsync(createdUser.Id);

        Assert.NotNull(user);
        Assert.That(user.Id, Is.EqualTo(createdUser.Id));
        Assert.That(user.Name, Is.EqualTo(createdUser.Name));
        Assert.That(user.Password, Is.EqualTo(createdUser.Password));

        await _userService.DeleteUserAsync(createdUser.Id);
    }

    //...other tests

}
```

Run the `CreateUser_ShouldReturnUser` and `GetUser_ShouldReturnUser` tests individually and check they pass.

---

## Hey...Didn't you say that the test should be responsible for the API calls it is testing?

You're right, I did. I have gotten ahead of myself and forgotten what the test is testing.

We've essentially **shifted the debate** from whether we should encapsulate API calls in a service layer to whether individual tests should call the API directly for their specific logic.  

---

### **Two Perspectives on This**  

#### **1️ The "Test Should Own the API Call" View (No Service Layer for Core Tests)**  
- The **CreateUser test** is specifically testing that the API correctly creates a user.  
- If we use `UserService.CreateUserAsync()`, we’re not directly testing the raw API, we’re testing our service layer.  
- This can **diffuse responsibility**, making the test **less explicit** about what it’s actually verifying.  

##### **When to use this?**  
- When testing the API **directly** as a contract.  
- When verifying that the API handles input/output correctly without additional abstraction.  
- If we want full **clarity on what’s being tested** (i.e., the HTTP request itself).  

---

#### **2️ The "Encapsulation is Good for All Tests" View (Using UserService)**  
- A service layer **removes duplication** across multiple tests.  
- It ensures tests remain **focused** on **expected behaviour**, not on making raw HTTP requests.  
- If the API **URL structure changes**, we only need to update the `UserService`, not every test.  

##### **When to use this?**  
- When writing **higher-level tests** focused on functionality, not HTTP mechanics (for instance, when we test that a user has something assigned to them, we don't want to manually write all the API calls).  
- When maintaining a **clean test suite** with **minimal duplication**.  
- When working on a large codebase where multiple tests depend on similar API calls.  

---

### **What’s Best?**  

It depends on **what we are testing**:  

1. **If we’re testing the API itself (contract tests)** then the test should call the API directly, without a service layer. The test should be responsible for `POST`, `GET`, and response handling.  

2. **If we’re writing functional tests (CRUD workflow tests)** then the service layer makes sense. The test should only care about whether a user is created, retrieved, or deleted—not about how the HTTP request is formed.  

---

## Handling `CreateUser`
Since `CreateUser` is a **core API operation**, we **want one test to directly test it** without using `UserService`. 

However, other tests that just **need a user to exist** (e.g., `GetUserById`) **should** use `UserService.CreateUserAsync()` for setup.  

Let's update our code so that our *creational* test doesn't rely on an abstraction:

```cs
[Test]
public async Task CreateUser_ShouldReturnUser()
{
    var newUser = UserFactory.CreateUser();

    var response = await _client.PostAsJsonAsync("api/Users", newUser);
    response.EnsureSuccessStatusCode();
    var createdUser = await response.Content.ReadFromJsonAsync<User>();

    Assert.NotNull(createdUser);
    Assert.Greater(createdUser.Id, 0);
    Assert.That(createdUser.Name, Is.EqualTo(newUser.Name));
    Assert.That(createdUser.Password, Is.EqualTo(newUser.Password));

    //We can use our service here, as it's not the part of the test we are interested in
    await _userService.DeleteUserAsync(createdUser.Id);

}
```

Run the test to ensure it passes.

Next, let's update our `GetUser` test to ensure the API call we are explictitly testing is directly called inside the test:

```cs
[Test]
public async Task GetUser_ShouldReturnUser()
{
    var newUser = UserFactory.CreateUser();
    //We can use our service here, as it's not the part of the test we are interested in
    var createdUser = await _userService.CreateUserAsync(newUser);

    var response = await _client.GetAsync($"api/Users/{createdUser.Id}");
    response.EnsureSuccessStatusCode();
    var user = await response.Content.ReadFromJsonAsync<User>();

    Assert.NotNull(user);
    Assert.That(user.Id, Is.EqualTo(createdUser.Id));
    Assert.That(user.Name, Is.EqualTo(createdUser.Name));
    Assert.That(user.Password, Is.EqualTo(createdUser.Password));

    //We can use our service here, as it's not the part of the test we are interested in
    await _userService.DeleteUserAsync(createdUser.Id);
}
```

---

## Complete Code

Here's what the updated tests look like:

```cs

using System.Net.Http.Json;
using UserApiTests.Factories;
using UserApiTests.Models;
using UserApiTests.Services;

namespace UserApiTests
{
    [TestFixture]
    public class UserApiTests
    {
        private HttpClient _client;
        private UserService _userService;

        [SetUp]
        public async Task SetUp()
        {
            _client = new HttpClient { BaseAddress = new Uri("https://localhost:7098/") };  //replace URL with whatever your URL is
            _userService = new UserService(_client);
        }

        [Test]
        public async Task CreateUser_ShouldReturnUser()
        {
            var newUser = UserFactory.CreateUser();

            var response = await _client.PostAsJsonAsync("api/Users", newUser);
            response.EnsureSuccessStatusCode();
            var createdUser = await response.Content.ReadFromJsonAsync<User>();

            Assert.NotNull(createdUser);
            Assert.Greater(createdUser.Id, 0);
            Assert.That(createdUser.Name, Is.EqualTo(newUser.Name));
            Assert.That(createdUser.Password, Is.EqualTo(newUser.Password));

            await _userService.DeleteUserAsync(createdUser.Id);

        }

        [Test]
        public async Task GetAllUsers_ShouldReturnUsers()
        {

            var newUser = UserFactory.CreateUser();
            var createdUser = await _userService.CreateUserAsync(newUser);

            var response = await _client.GetAsync("api/Users");
            response.EnsureSuccessStatusCode();

            var users = await response.Content.ReadFromJsonAsync<User[]>();

            Assert.NotNull(users);
            Assert.IsNotEmpty(users);

            await _userService.DeleteUserAsync(createdUser.Id);
        }

        [Test]
        public async Task GetUser_ShouldReturnUser()
        {
            var newUser = UserFactory.CreateUser();
            var createdUser = await _userService.CreateUserAsync(newUser);

            var response = await _client.GetAsync($"api/Users/{createdUser.Id}");
            response.EnsureSuccessStatusCode();
            var user = await response.Content.ReadFromJsonAsync<User>();

            Assert.NotNull(user);
            Assert.That(user.Id, Is.EqualTo(createdUser.Id));
            Assert.That(user.Name, Is.EqualTo(createdUser.Name));
            Assert.That(user.Password, Is.EqualTo(createdUser.Password));

            await _userService.DeleteUserAsync(createdUser.Id);
        }

        [Test]
        public async Task UpdateUser_ShouldModifyUser()
        {
            var newUser = UserFactory.CreateUser();
            var createdUser = await _userService.CreateUserAsync(newUser);

            var updatedUser = new User {Id = createdUser.Id, Name = "JohnDoeUpdated", Password = "NewPass123" };

            var response = await _client.PutAsJsonAsync($"api/Users/{createdUser.Id}", updatedUser);
            response.EnsureSuccessStatusCode();

            var fetchedUser = await _userService.GetUserByIdAsync(createdUser.Id); 

            Assert.NotNull(fetchedUser);
            Assert.That(fetchedUser.Name, Is.EqualTo(updatedUser.Name));
            Assert.That(fetchedUser.Password, Is.EqualTo(updatedUser.Password));

            await _userService.DeleteUserAsync(createdUser.Id);
        }

        [Test]
        public async Task DeleteUser_ShouldRemoveUser()
        {
            var newUser = UserFactory.CreateUser();
            var createdUser = await _userService.CreateUserAsync(newUser);

            var response = await _client.DeleteAsync($"api/Users/{createdUser.Id}");
            response.EnsureSuccessStatusCode();

            var getUserResponse = await _client.GetAsync($"api/Users/{createdUser.Id}");
            Assert.That(getUserResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
        }

        [Test]
        public async Task LoginUser_ShouldBeSuccessful()
        {
            var newUser = UserFactory.CreateUser();
            var createdUser = await _userService.CreateUserAsync(newUser);

            var response = await _client.PostAsJsonAsync($"api/Users/login", newUser);
            response.EnsureSuccessStatusCode();

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

            Assert.NotNull(loginResponse);
            Assert.That(loginResponse.Id, Is.EqualTo(createdUser.Id));

            await _userService.DeleteUserAsync(createdUser.Id);

        }

        [TearDown]
        public async Task TearDown()
        {
            _client.Dispose();
        }
    }
}

```
---

You can find the code [here](./code/UserApiTests/Patterns/).

[>> A Different HttpClient](./restsharp.md)
