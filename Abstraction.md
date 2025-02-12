# **Abstracting API Paths to Remove Duplication**  

Hardcoding `"api/Users"` in multiple places creates duplication and makes maintenance harder. If the endpoint changes, we'd have to update every instance manually.  

To solve this, we can encapsulate API paths in a **dedicated class** that centralises all route definitions. This follows the **DRY (Don't Repeat Yourself)** principle and makes code easier to maintain.  

---

## **Create an `ApiRoutes` Static Class**  
We'll define a static class to store all API paths.  

1. Create a ne folder, `Helpers`
2. Add a new class, `ApiRoutes`:

```csharp
public static class ApiRoutes
{
    private const string Base = "api/Users";

    public static string Users => Base;
    public static string UserById(long id) => $"{Base}/{id}";
    public static string Login => $"{Base}/login";
}
```

### **Why This Is Better**:  
1. **No more "magic strings"** → All endpoints are in one place.  
2. **Easier maintenance** → If the API path changes, update it in one place.  
3. **Cleaner tests and services** → No hardcoded URLs in multiple locations.  

---

## **Updating `UserService` to Use `ApiRoutes`**  
Now, instead of `"api/Users"` appearing multiple times, we use `ApiRoutes.Users` and so on:  

```csharp
public async Task<User> CreateUserAsync(User user)
{
    var request = new RestRequest(ApiRoutes.Users).AddJsonBody(user);
    return await _client.PostAsync<User>(request);
}

public async Task<User> GetUserByIdAsync(long userId)
{
    var request = new RestRequest(ApiRoutes.UserById(userId));
    return await _client.GetAsync<User>(request);
}

public async Task DeleteUserAsync(long userId)
{
    var request = new RestRequest(ApiRoutes.UserById(userId));
    await _client.DeleteAsync(request);
}
```

---

## **Updating Tests to Use `ApiRoutes`**  
Instead of hardcoding paths in tests, we now use the same route definitions:  

```csharp
[Test]
public async Task CreateUser_ShouldReturnUser()
{
    var newUser = UserFactory.CreateUser();
    var createdUser = await _userService.CreateUserAsync(newUser);

    var response = await _client.PostAsJsonAsync(ApiRoutes.Login, newUser);
    response.EnsureSuccessStatusCode();

    await _userService.DeleteUserAsync(createdUser.Id);
}
```

---

## **Key Points**  
- This pattern **reduces duplication** and makes maintenance easier.  
- If the API base path changes, update `ApiRoutes` - everything else stays the same.  
- Tests and services now **share the same route definitions**, reducing inconsistencies.  

---

## Exercise

We still have a hardcoded URL for the root (the localhost). Think of two different ways we could abstract this so it's not hardcoded.

Implement one of them!

---

[>> The Unhappy Path](./unhappy.md)
