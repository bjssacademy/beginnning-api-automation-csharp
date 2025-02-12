# Introducing RestSharp

`HttpClient` is a reliable, built-in choice for making HTTP requests, but **RestSharp** provides a more streamlined experience, particularly for API testing. You could write your own wrapper for the HttpClient, but why do all that work if you don't need to (and most of the time you won't)?  

## Why Use RestSharp?

### **1. Simplifies Syntax**  
RestSharp reduces boilerplate code and makes API requests more readable.  

**Example Comparison (POST Request)**  

**Using `HttpClient` (more verbose)**  
```csharp
var response = await _client.PostAsJsonAsync("api/Users", user);
response.EnsureSuccessStatusCode();
var createdUser = await response.Content.ReadFromJsonAsync<User>();
```

**Using `RestSharp` (more concise)**  
```csharp
var request = new RestRequest("api/Users").AddJsonBody(user);
var createdUser = await _client.PostAsync<User>(request);
```
RestSharp removes the need to manually handle serialization and deserialization, making the code easier to read.  

### **2. Built-in Support for Request Chaining**  
RestSharp allows requests to be built up in a **fluent style**, reducing clutter in test methods.  

### **3. Better for API Testing**  
- **Automatic serialization** – Converts objects to JSON without extra steps.  
- **Easier response handling** – `client.PostAsync<User>(request)` automatically deserializes responses into objects.  
- **More flexible configuration** – Built-in features for retries, timeouts, and authentication.  

#### **4. Improved Maintainability**  
With RestSharp, test code is easier to maintain and update. If API endpoints change, adjustments are simpler since the request setup is more declarative.  

---

## **Refactoring `UserService` to Use RestSharp**  

### **Step 1: Install RestSharp**  
If you haven't already, install the **RestSharp** NuGet package:  
1. Open your test project in Visual Studio.  
2. Go to **Tools** > **NuGet Package Manager** > **Manage NuGet Packages for Solution**.  
3. Search for **RestSharp** and install it.  
   
---

### **Step 2: Update `UserService` to Use RestSharp**  

Here’s the refactored `UserService` that replaces `HttpClient` with `RestSharp`:  

```csharp
using RestSharp;
using System.Threading.Tasks;
using UserApiTests.Models;

namespace UserApiTests.Services
{
    public class UserService
    {
        private readonly RestClient _client;

        public UserService(RestClient client)
        {
            _client = client;
        }

        public async Task<User> CreateUserAsync(User user)
        {
            var request = new RestRequest("api/Users").AddJsonBody(user);
            return await _client.PostAsync<User>(request);
        }

        public async Task<User> GetUserByIdAsync(long userId)
        {
            var request = new RestRequest($"api/Users/{userId}");
            return await _client.GetAsync<User>(request);
        }

        public async Task DeleteUserAsync(long userId)
        {
            var request = new RestRequest($"api/Users/{userId}");
            await _client.DeleteAsync(request);
        }
    }
}
```

---

### **Explanation**  

#### **1. Constructor Uses `RestClient` Instead of `HttpClient`**  
```csharp
private readonly RestClient _client;

public UserService(RestClient client)
{
    _client = client;
}
```
- `RestClient` replaces `HttpClient`, making API interactions more concise.  
- This follows the **dependency injection** pattern, keeping it flexible for testing.  

#### **2. Simpler Request Creation**  
Each method creates a `RestRequest` instead of manually formatting JSON:  
```csharp
var request = new RestRequest("api/Users").AddJsonBody(user);
```
- `AddJsonBody(user)` handles serialization automatically.  
- The request method (`PostAsync`, `GetAsync`, `DeleteAsync`) determines the HTTP verb.  

#### **3. Response Handling is Cleaner**  
```csharp
return await _client.PostAsync<User>(request);
```
- **Automatically deserializes JSON into the `User` object** without extra steps.  
- **No need for `EnsureSuccessStatusCode()`**, since RestSharp throws on failure.  

---

### **Step 3: Update Dependency Injection in Tests**  

Wherever `UserService` is instantiated, update it to use `RestClient`:  

```csharp
var options = new RestClientOptions("http://localhost:5000"); // replace with your base url
var restClient = new RestClient(options);
var userService = new UserService(restClient);
```
- The **base URL** is now set once in `RestClientOptions`, making requests cleaner.  

---

## **Refactoring `UserApiTests` to Use RestSharp**  

Now that `UserService` has been updated to use RestSharp, we need to refactor `UserApiTests` to:  
1. Use `RestClient` instead of `HttpClient`.  
2. Update test methods to use `UserService` for API interactions.  

---

### **Updated `UserApiTests` Class**  

```csharp
using RestSharp;
using System.Net.Http.Json;
using UserApiTests.Factories;
using UserApiTests.Models;
using UserApiTests.Services;

namespace UserApiTests
{
    [TestFixture]
    public class UserApiTests
    {
        private RestClient _client;
        private UserService _userService;

        [SetUp]
        public async Task SetUp()
        {
            var options = new RestClientOptions("https://localhost:7098/");  //replace URL with whatever your URL is
            _client = new RestClient(options);
            _userService = new UserService(_client);
        }

        [Test]
        public async Task CreateUser_ShouldReturnUser()
        {
            var newUser = UserFactory.CreateUser();

            var request = new RestRequest("api/Users").AddJsonBody(newUser);
            var response = await _client.ExecutePostAsync<User>(request);

            if (!response.IsSuccessful || response.Data == null)
            {
                throw new Exception($"Failed to create user: {response.StatusCode} - {response.ErrorMessage}");
            }

            var createdUser = response.Data;

            Assert.NotNull(createdUser);
            Assert.Greater(createdUser.Id, 0);
            Assert.That(createdUser.Name, Is.EqualTo(newUser.Name));
            Assert.That(createdUser.Password, Is.EqualTo(newUser.Password));

            await _userService.DeleteUserAsync(createdUser.Id);

        }

        //...other methods

    }
}
```

Full code [here](./code/UserApiTests/RestSharp/)

---

## **What Happened to `response.EnsureSuccessStatusCode();`?**  

In the original `HttpClient` implementation, this line ensures that the HTTP request was successful:  

```csharp
response.EnsureSuccessStatusCode();
```
- If the response has a **4xx or 5xx status code**, it throws an exception.  
- This prevents further execution if the request fails.  

#### **How Does RestSharp Handle This?**  
RestSharp does **not** automatically throw exceptions for non-successful responses. 
We have to do a bit more work.

- If a request fails (e.g., 404, 500), `client.PostAsync<T>(request)` **returns `null`** instead of throwing.  
- To explicitly check for errors, you must inspect the response’s `IsSuccessStatusCode` or `StatusCode`.

---

### **How Do We Handle Errors in RestSharp?**  

There are two ways to ensure failures are caught.

#### **1. Check the Response Before Using the Data**  
Update `UserService` methods to check the response before returning data:  

```csharp
public async Task<User> CreateUserAsync(User user)
{
    var request = new RestRequest("api/Users").AddJsonBody(user);
    var response = await _client.ExecutePostAsync<User>(request);

    if (!response.IsSuccessful || response.Data == null)
    {
        throw new Exception($"Failed to create user: {response.StatusCode} - {response.ErrorMessage}");
    }

    return response.Data;
}
```
- This **ensures** that a failure does not return `null` silently.  
- If something goes wrong, the test will **fail explicitly** with a useful error message.  

---

#### **2. Validate the Response in Tests**  
In test methods, check that the response was successful before proceeding:  

```csharp
[Test]
public async Task CreateUser_ShouldReturnUser()
{
    var newUser = UserFactory.CreateUser();
    var createdUser = await _userService.CreateUserAsync(newUser);

    Assert.NotNull(createdUser, "User creation failed, response was null.");
    Assert.Greater(createdUser.Id, 0, "User ID should be greater than 0.");
    Assert.That(createdUser.Name, Is.EqualTo(newUser.Name));
    Assert.That(createdUser.Password, Is.EqualTo(newUser.Password));

    await _userService.DeleteUserAsync(createdUser.Id);
}
```
- If the API request fails, `createdUser` will be `null`, and the test will fail early.  

---

## **Difference Between `ExecutePostAsync<T>(request)` and `PostAsync<T>(request)` in RestSharp**  

RestSharp provides **multiple ways** to make API calls, and while both `ExecutePostAsync<T>(request)` and `PostAsync<T>(request)` send a `POST` request, they differ in how they handle responses.  

---

### **1. `ExecutePostAsync<T>(request)` (Used in the Test)**  
```csharp
var response = await _client.ExecutePostAsync<User>(request);
```
**Returns a `RestResponse<User>` object**, which includes:  
- `response.IsSuccessful` → **Boolean** indicating if the request was successful.  
- `response.StatusCode` → The HTTP status code (e.g., `200 OK`, `400 Bad Request`).  
- `response.Data` → The deserialized `User` object **(or `null` if deserialization fails)**.  
- `response.ErrorMessage` → Any error encountered during the request.  

> :exclamation: **You must manually check `response.IsSuccessful` and `response.Data` before using it!**  
Example:
```csharp
if (!response.IsSuccessful || response.Data == null)
{
    throw new Exception($"Failed to create user: {response.StatusCode} - {response.ErrorMessage}");
}
```

---

### **2. `PostAsync<T>(request)` (Used in the `UserService`)**  
```csharp
return await _client.PostAsync<User>(request);
```
**Directly returns the deserialized object (`User`) instead of a `RestResponse<User>`.**  
- If the request is **successful**, it returns the `User` object.  
- If the request fails (e.g., 404, 500), it **returns `null` instead of throwing an exception**.  

> :exclamation: **No built-in error handling** – it doesn't give you access to `StatusCode`, `ErrorMessage`, etc.  

---

### **Which One Should You Use?**  

| Method | Returns | Handles Errors? | Use Case |
|--------|--------|---------------|----------|
| `ExecutePostAsync<T>()` | `RestResponse<T>` | Yes (gives status codes and errors) | When you need **full response details**, including failures. |
| `PostAsync<T>()` | `T` (e.g., `User`) | No (returns `null` on failure) | When you only care about the result, not the response details. |

---

### **Why Use `ExecutePostAsync<T>()` in the Test?**  
In tests, we want **more control over the response** to handle failures explicitly.  

Example:
```csharp
var response = await _client.ExecutePostAsync<User>(request);
Assert.IsTrue(response.IsSuccessful, $"Request failed: {response.StatusCode}");
Assert.NotNull(response.Data, "Response body was null.");
Assert.Greater(response.Data.Id, 0);
```
This ensures we **fail fast** if something goes wrong.  

---

### **Why Use `PostAsync<T>()` in `UserService`?**  
In the service layer, we typically **only care about the final object**:  
```csharp
public async Task<User> CreateUserAsync(User user)
{
    return await _client.PostAsync<User>(new RestRequest("api/Users").AddJsonBody(user));
}
```
- If the request is successful, we get the `User` object.  
- If it fails, it returns `null`—which we can handle appropriately.  

---

### **Overall**  

We should expect our service to be successful - we don't really want to check every single request for its status, that's why we have explicit tests for it.

- Use **`ExecutePostAsync<T>()` in tests** when you need **full response details**.  
- Use **`PostAsync<T>()` in the service layer** when you only need the **resulting object**.  
- If `PostAsync<T>()` returning `null` is an issue, **wrap it in error handling** in `UserService`.  

---

## Why Use RestSharp And Our Service This Way?

We've gone through a lot of concepts in this section. As with everything, there will be opinions on which is correct.

But for what we are doing right now, having explicit control inside the test when testing an endpoint with the ability to check status codes, and handing off to a service when we just expect it to work without issue strikes the right balance.

YMMV.

---

[>> Abstraction](./Abstraction.md)



