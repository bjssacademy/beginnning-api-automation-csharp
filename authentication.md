# Authentication with JWT

Authentication is a critical aspect of securing applications, and JSON Web Token (JWT) has become a popular choice for implementing secure authentication mechanisms.

In this blog section, we’ll explore JWT token authentication in C#, providing code snippets and explanations to demystify this crucial concept.

Whether you’re a fresher or a developer looking to enhance your authentication knowledge, let’s embark on a journey to understand JWT in the context of C#.

## Background: Understanding JWT
JSON Web Tokens (JWT) are compact, URL-safe means of representing claims between two parties. These claims are typically used to identify the user and provide additional information such as roles and permissions.

JWTs consist of three parts: Header, Payload, and Signature. The Header and Payload are Base64Url encoded JSON strings, and the Signature is used to verify the authenticity of the token.

A **JWT (JSON Web Token)** is a bit like a cinema ticket. It proves who you are and what you’re allowed to see.  

### **How It Works**  
A JWT has **three parts**:  

1. **Header** – Like the type of ticket (e.g., "IMAX, 3D")  
2. **Payload** – Your info (e.g., "Name: John, Seat: A12")  
3. **Signature** – A special stamp to prevent fakes  

Example JWT:  
```
HEADER.PAYLOAD.SIGNATURE
```
Looks like this:  
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9  (Header)
.
eyJ1c2VySWQiOiIxMjM0Iiwicm9sZSI6IlVzZXIifQ  (Payload)
.
h4xWc3Rf8uS_kjWDOueMqvA2eX0OxmY  (Signature)
```

- The **Header** and **Payload** are just text, but scrambled (Base64).  
- The **Signature** is like a secret stamp—if someone changes the Payload, the stamp breaks.  

### **Why Use It?**  
When you log in, the server gives you a JWT (like a ticket). You show this ticket when calling other APIs, and they trust it because of the signature.

To achieve this, you'll need to:

1. **Generate a JWT token** in your `LoginUser` endpoint.
2. **Use `[Authorize]` attributes** to protect other endpoints.
3. **Test the authentication flow** by making requests with and without the token.

---

## Updating the `LoginUser` Method to Return a Token

You'll need go to your UserApiAuth to install `Microsoft.AspNetCore.Authentication.JwtBearer`,`System.IdentityModel.Tokens.Jwt` and `Microsoft.IdentityModel.Tokens` from Nuget.

> NOTE: Make sure they are version 8.X!

Update the `LoginUser` method to return a JWT:

```csharp

[HttpPost("login")]
public async Task<IActionResult> LoginUser(User user)
{
    if (string.IsNullOrWhiteSpace(user.Name) || string.IsNullOrWhiteSpace(user.Password))
    {
        return BadRequest(new { message = "Name and/or Password cannot be empty." });
    }

    var valid = _context.Users.FirstOrDefault(x => x.Name == user.Name && x.Password == user.Password);
    if (valid == null)
    {
        return Unauthorized();
    }

    var token = GenerateJwtToken(valid);

    return Ok(new { token });
}

private string GenerateJwtToken(User user)
{
    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your_super_secret_key_here_at_least_32_chars_long"));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Name),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var token = new JwtSecurityToken(
        issuer: "your_issuer",
        audience: "your_audience",
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(30),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

---

## Protecting Endpoints with `[Authorize]`

Now, let's protect another endpoint, e.g., retrieving all users:

```csharp
[Authorize]
[HttpGet]
public async Task<ActionResult<IEnumerable<User>>> GetUsers()
{
    return await _context.Users.ToListAsync();
}
```

---

## Configuring Authentication in `Startup.cs` (or `Program.cs` in .NET 6+)

You'll need to configure JWT authentication:

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "your_issuer",
            ValidAudience = "your_audience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your_super_secret_key_here_at_least_32_chars_long"))
        };
    });
```

And in `Program.cs` or `Startup.cs`, make sure to call:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

---

## Checking Swagger

If you now run your API and try to use the GET endpoint, you will get a 401 response:

![alt text](image-27.png)

---

## Writing a Test for Authenticated Requests

Our GetAllUsers test also has a problem now:

![alt text](image-28.png)

### Check For Unathorised

First, let's add a test to ensure if we are not authorised, that we get a `401 - Unathorized` response:

```csharp
[Test]
public async Task GetUsers_ShouldReturnUnauthorized_IfNoTokenProvided()
{
    var request = new RestRequest(ApiRoutes.Users, Method.Get);
    var response = await _client.ExecuteAsync(request);

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
}
```

### Authorised Test

Now let's update our test for getting all users.

First, the API has changed its response from returning a `long Id `to `string Token`. Update the `LoginResponse.cs` class:

```cs
internal class LoginResponse
{
    public string Token { get; set; }
}
```

Now we can update our test:

```csharp
[Test]
public async Task GetAllUsers_ShouldReturnUsers()
{

    var newUser = UserFactory.CreateUser();
    var createdUser = await _userService.CreateUserAsync(newUser);

    var loginRequest = new RestRequest(ApiRoutes.Login)
        .AddJsonBody(new { Name = newUser.Name, Password = newUser.Password });

    var loginResponse = await _client.ExecutePostAsync<LoginResponse>(loginRequest);
    loginResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

    var token = loginResponse.Data.Token;

    var request = new RestRequest(ApiRoutes.Users);
    request.AddHeader("Authorization", $"Bearer {token}");
    
    var response = await _client.ExecuteGetAsync<List<User>>(request);

    if (!response.IsSuccessful || response.Data == null)
    {
        throw new Exception($"Failed to get users: {response.StatusCode} - {response.ErrorMessage}");
    }

    var users = response.Data;

    Assert.NotNull(users);
    Assert.IsNotEmpty(users);

    await _userService.DeleteUserAsync(createdUser.Id);
}
```

> You will need to update your `Loginuser_ShouldBeSuccessful()` test, update the following lines:

```cs
Assert.NotNull(loginResponse);
loginResponse.Token.ShouldNotBeNullOrEmpty(); //Update this line, replacing the Assert()
```

Run the test and make sure it passes!

---

## **How Does It Work?**  
When a client calls a protected API endpoint:  
1. The client **retrieves the JWT token** from the login response.  
2. The client sends **the token in the `Authorization` header`** using the `Bearer` scheme.  
3. The server **validates the token** and processes the request only if it’s valid.  

---

## **Example: Adding the Token in a Request**  

### **Example Login Request (Getting the Token)**  
#### **Request**  
```http
POST /api/users/login HTTP/1.1
Content-Type: application/json

{
    "name": "testuser",
    "password": "password123"
}
```

#### **Response (Token in JSON)**
```json
{
    "token": "eyJhbGciOiJIUzI1NiIsInR..."
}
```

---

### **Making an Authenticated Request (Using the Token)**  
Once you have the token, **send it in the `Authorization` header`** for future requests:  

#### **Request**  
```http
GET /api/orders HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR...
```

The `Bearer` scheme tells the API that the value after `Bearer` is a **JWT token**.

---

## **How the Server Handles It**
On the server, when an endpoint has the `[Authorize]` attribute:
```csharp
[Authorize]
[HttpGet("orders")]
public IActionResult GetOrders()
{
    return Ok(new { orders = new[] { "Order1", "Order2" } });
}
```
1. **ASP.NET Core reads the `Authorization` header`.**  
2. **It validates the token** (checking expiration, signature, etc.).  
3. If **valid**, the request proceeds. If **invalid**, it returns `401 Unauthorized`.  

---

## Exercise

1. Update the UserAuthApi endpoints for getting a single user, updating and deleting a user, so that they require authorization.

2. Update your tests to provide authorization.

---

## The End

There's so much more we could do here - from making sure only the logged in user can delete their own account, to securing our endpoints using Role Based Authentication (RBAC). but for now this article has served its purpose of dealing with an introduction to testing APIs with C#.

We've looked at manual testing with Bruno, built our own in-memory API, made our tests atomic, looked at third-party integration, created a stub we turned into a mock, and a brief look at authentication.

If you want to try restricting who can delete an account, here's some example code :

```cs
[Authorize]
[HttpDelete("{id}")]
public async Task<IActionResult> DeleteUser(long id)
{
    // 1. Extract user ID from the JWT token
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
    if (userIdClaim == null)
    {
        return Unauthorized(new { message = "Invalid token." });
    }

    if (!long.TryParse(userIdClaim.Value, out long loggedInUserId))
    {
        return Unauthorized(new { message = "Invalid user ID in token." });
    }

    // 2. Ensure the logged-in user is deleting their own account
    if (loggedInUserId != id)
    {
        return Forbid(); // 403 Forbidden if user is trying to delete someone else
    }

    // 3. Find the user in the database
    var user = await _context.Users.FindAsync(id);
    if (user == null)
    {
        return NotFound();
    }

    // 4. Delete the user
    _context.Users.Remove(user);
    await _context.SaveChangesAsync();

    return NoContent();
}
```