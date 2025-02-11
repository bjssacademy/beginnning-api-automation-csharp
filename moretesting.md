# CRUD Testing an API

This focuses on **happy-path testing** (successful scenarios only) for the **Users API** in a .NET 8 Web API using **NUnit and HttpClient**. 

Here's what we are going to do:  

1. **Set up an NUnit Test Project**  
2. **Write API tests** for:  
   - Adding a user (`POST /api/Users`)  
   - Retrieving all users (`GET /api/Users`)  
   - Updating a user (`PUT /api/Users/{id}`)  
   - Deleting a user (`DELETE /api/Users/{id}`)  

## **1. Create an NUnit Test Project**  

### **Step 1: Open Visual Studio and Create an NUnit Test Project**  
1. Open **Visual Studio**  
2. Select **"Create a new project"**  
3. Choose **"NUnit3 Test Project"**, then click **"Next"**  
4. Name the project **`UserApiTests`**, then click **"Create"**  

### **Step 2: Add Required Dependencies**  
1. **Right-click** the project in **Solution Explorer**  
2. Select **"Manage NuGet Packages"**  
3. Install the following packages:  
   - `System.Net.Http.Json`  
   - `Microsoft.NET.Test.Sdk` (if not already installed)  
   - `NUnit`   (if not already installed)  
   - `NUnit3TestAdapter`   (if not already installed)  

---

## **2. Define the User Model**  

Create a new file **`User.cs`** inside the test project and add:  

```csharp
public class User
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Password { get; set; }
}
```

---

## **3. Write API Tests Using HttpClient**  

### **Step 1: Create a Test Class**  

In **Solution Explorer**, open `UnitTest1.cs` and rename it to **`UserApiTests.cs`**.  

Replace the contents with the following, replacing the localhost URL with whatever your is:  

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using NUnit.Framework;

namespace UserApiTests
{
    [TestFixture]
    public class UserApiTests
    {
        private HttpClient _client;

        [SetUp]
        public void Setup()
        {
            _client = new HttpClient { BaseAddress = new Uri("http://localhost:5000/api/Users/") };  //replace URL with whatever your URL is
        }

        [Test]
        public async Task CreateUser_ShouldReturnUser()
        {
            var newUser = new User { Name = "JohnDoe", Password = "SecurePass123" };

            var response = await _client.PostAsJsonAsync("", newUser);
            response.EnsureSuccessStatusCode();

            var createdUser = await response.Content.ReadFromJsonAsync<User>();

            Assert.NotNull(createdUser);
            Assert.Greater(createdUser.Id, 0);
            Assert.AreEqual(newUser.Name, createdUser.Name);
        }

        [Test]
        public async Task GetAllUsers_ShouldReturnUsers()
        {
            var response = await _client.GetAsync("");
            response.EnsureSuccessStatusCode();

            var users = await response.Content.ReadFromJsonAsync<User[]>();

            Assert.NotNull(users);
            Assert.IsNotEmpty(users);
        }

        [Test]
        public async Task UpdateUser_ShouldModifyUser()
        {
            var updatedUser = new User { Id = 1, Name = "JohnDoeUpdated", Password = "NewPass123" };

            var response = await _client.PutAsJsonAsync("1", updatedUser);
            response.EnsureSuccessStatusCode();

            var getUserResponse = await _client.GetAsync("1");
            var fetchedUser = await getUserResponse.Content.ReadFromJsonAsync<User>();

            Assert.NotNull(fetchedUser);
            Assert.AreEqual("JohnDoeUpdated", fetchedUser.Name);
        }

        [Test]
        public async Task DeleteUser_ShouldRemoveUser()
        {
            var response = await _client.DeleteAsync("1");
            response.EnsureSuccessStatusCode();

            var getUserResponse = await _client.GetAsync("1");
            Assert.AreEqual(System.Net.HttpStatusCode.NotFound, getUserResponse.StatusCode);
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
        }
    }
}
```

> :exclamation: Remember that we are running against an in-memory application, which means that if it restarts it will have *zero* users in it. That makes these tests fragile, and dependent on each other to run in an **exact** order (for example, you have to add a user before you can edit or delete a user, otherwise your tests will fail). That is **bad**, but it's not the objective of this section.
>
> More advanced topics will cover how we'd make these tests *atomic* and be able to be run in any order.

---

## **4. Running the Tests**  

### **Step 1: Open Test Explorer**  
1. Go to **Test → Test Explorer** in Visual Studio  

### **Step 2: Run the Tests**  
1. Click **Run All**  
2. Ensure all tests **pass**  

---

> :exclamation: Please remember, this tutorial **focuses on happy-path scenarios only**, ensuring the API performs correctly under expected conditions.

---

### **Next Steps: Labs & Exercises**  

Now that you've successfully tested the **happy-path CRUD operations** for the `Users` API, here are some hands-on labs and exercises to deepen your understanding of **API testing with NUnit and HttpClient**.  

---

## ** Lab 1: Add Login Tests (`POST /api/Users/login`)**  
**Goal:** Write a test to verify the login functionality.  

### **Tasks:**  
1. Modify the `User` model to include login request/response handling if needed.  
2. Create a **test method** for `POST /api/Users/login` that:  
   - Sends a **valid username and password**.  
   - Asserts that the response is **successful** (e.g., contains a token or user details).  
3. Run the test and confirm that login works correctly.  

📌 **Bonus:** Try logging in with an **incorrect password** and observe the response.  

---

## ** Lab 2: Validate User Data Before Making API Calls**  
**Goal:** Improve test reliability by validating test inputs.  

### **Tasks:**  
1. Update `UpdateUser_ShouldModifyUser` to check:  
   - The **user exists** before updating.  
   - The **ID is valid** before sending the request.  

📌 **Bonus:** Try sending a request with an **empty name** and check if the API rejects it properly.  

---
