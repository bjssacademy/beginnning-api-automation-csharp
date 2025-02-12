# Atomicity - Our Tests Don't Have it

We've got a problem as alluded to before - our tests are not *atomic*. What does that mean?

The tests are **not atomic** because they follow a **linear dependency** — each one requires something to exist before it can proceed. If a test depends on a previous test to create data, then running them out of order causes failures.  

It's much like building a house -  
- You **lay the foundation** before **building walls**.  
- The **roof needs walls** to rest on.  
- If you try to **install windows before the walls exist**, you get **errors**.  

Each step depends on the **previous step's success**, making them **order-dependent** rather than independent. 

> KEY POINT **Atomic tests should create and clean up their own data** so they can run in **any order** without breaking.

---

## Okay, so what do we do?

To make tests **atomic** for the `UserAuthApi`, each test must be **self-contained**, meaning it should:  

1. **Create its own test data** – Instead of relying on a user created in a previous test, each test should **add** its own user before performing actions.  
2. **Use unique data** – Tests should generate **distinct usernames or IDs** to avoid conflicts when running in parallel.  
3. **Clean up after execution** – If a test adds a user, it should **delete** that user at the end to maintain a clean test environment.  
4. **Avoid shared state** – Tests shouldn’t assume any data **exists beforehand**; they should set up everything they need and not depend on other tests to do it.  

If we follow these principles then each test becomes **independent**, meaning they can run **in any order** without failures (well, those caused by missing or conflicting data at least).

## Right...sounds great. How?

Sounds easy enough. Let's look at some practical steps to how we can actually achieve this.

### **1. Arrange-Act-Assert (AAA) Pattern**  
Each test should follow a structured approach:  
- **Arrange** – Set up the necessary test data (e.g., create a unique user).  
- **Act** – Perform the actual API request.  
- **Assert** – Verify the expected response.  

This ensures that every test is **self-contained and predictable**.  


### **2. Test Data Management with the Factory Pattern**  
Instead of hardcoding test users, we use a **test data factory** to generate **unique** users per test.  
- A `UserFactory` class to create **random usernames or IDs**, ensuring no conflicts between tests.  
    - This prevents tests from interfering with each other when running in **parallel**.  

### **3. Setup & Teardown (Fixture Setup Pattern)**  
To maintain a clean test environment:  
- **Before each test** (`[SetUp]`), create test data **specific to those tests**.  
- **After each test** (`[TearDown]`), delete any created users.  
    - This avoids **leftover data** that could impact other tests.  

### **4. Dependency Injection for Test Clients**  
Instead of using a global `HttpClient`, we create a **new client instance per test** or **inject a test client** configured for the test environment.  
- This ensures **isolation** and prevents **state leakage** between tests.  

### **5. Idempotent Operations (State Reset Paradigm)**  
Tests should be designed so that re-running them doesn’t cause unintended failures.  
- Instead of assuming a user exists, each test **creates its own user**.  
- Instead of assuming an ID is valid, each test **fetches or generates one dynamically**.  

### **6. Parallel Execution with Unique Identifiers**  
- By ensuring all test data is **unique**, we enable **safe parallel test execution**.  
- Example: Appending a timestamp or GUID to usernames prevents duplicate user issues.  

> I know that is a wall of text, but it's important to know that we can actually achieve the same outcome in different ways using different approaches.

---

## Simple Arrange-Act-Assert

Let's start in the simplest way, by using the AAA pattern. Looking at our GetUser test, it is dependent on the CreateUser test being executed before it.

To make it atomic, we need to create a new, unique user (our Arrange step), then call using the new Id created for that user (the Act step), and then check the details returned are correct (the Assert step).

In the most basic way we can update out test like so:

```c#
[Test]
public async Task GetUser_ShouldReturnUser()
{
    // Arrange: Create a new user John Doe99 via POST request
    var newUser = new User
    {
        Name = "John Doe99",
        Password = "SecurePass123"
    };

    // Create the user (POST /api/Users)
    var createResponse = await _client.PostAsJsonAsync("api/Users", newUser);
    createResponse.EnsureSuccessStatusCode();

    // Act: Get the ID of the newly created user
    var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();

    // Get user by ID using the returned ID from creation
    var getResponse = await _client.GetAsync($"api/Users/{createdUser.Id}");
    getResponse.EnsureSuccessStatusCode();

    // Get the user details
    var user = await getResponse.Content.ReadFromJsonAsync<User>();

    // Assert: Ensure the returned user matches the expected data
    Assert.NotNull(user);
    Assert.AreEqual(createdUser.Id, user.Id); // Ensure the ID matches
    Assert.AreEqual("John Doe99", user.Name); // Ensure the name is John Doe99
    Assert.AreEqual("SecurePass123", user.Password); // Ensure the password is SecurePass123
}

```

You can run this test multiple times now, and it will always pass (as long as the API is running). Give it a try. Try restarting the UserAuthAPI project and running this test on its own a few times.

### A Couple Of Problems

We have to take a step back here and think about what we are doing. 

1. We can now add multiple new users with the name `John Doe99` and the password `SecurePass123`
2. We don't remove the user from the database
3. Since we can now have multiple users identical name and passwords, if any other test uses those login details, it *may* return the wrong user

There are two approaches we can take here - 

1. The test deletes the user when it finishes
2. We make the username unique so that it will never be identical to some other user  in the database.

---

### So Which Do We Use?

In the great world of testing...it depends.

#### **1. The Test Deletes the User After It Finishes**

**Pros:**
- **Ensures Cleanup**: Automatically removes the user from the database after the test, ensuring no leftover data that could interfere with other tests.
- **No Need for Unique Data**: You can create users with common details (e.g., "John Doe99" with password "SecurePass123") without worrying about conflicts.
- **Simplicity**: Keeps the test straightforward, without needing to modify how the user data is generated.

**Cons:**
- **Potential for Test Failures**: If the cleanup step fails for any reason (e.g., if an exception occurs while deleting the user), there could be leftover data that causes issues in subsequent tests. It's important to ensure that the cleanup is robust.
- **Performance**: If there are many tests that create and delete users, this can introduce overhead, especially if the database cleanup happens for every individual test. It might not be noticeable in small test suites, but could slow down larger test runs.
- **State Dependency**: If the cleanup step is skipped or fails in an earlier test, the state might persist, causing test failures or inconsistencies across different environments (local vs CI/CD).



#### **2. Make the Username Unique (e.g., "John Doe99_{Guid}" or "John Doe99_{Timestamp}")**

**Pros:**
- **Guaranteed Uniqueness**: Since each username is generated dynamically, you eliminate the risk of name conflicts across tests, ensuring that users are always distinct.
- **No Cleanup Needed**: With unique usernames, there's no need to delete users after tests. This makes the tests self-contained and independent from one another.
- **Improved Performance in Larger Test Suites**: Avoiding the overhead of creating/deleting users for each test can improve performance, especially in larger test suites where cleanup might take a significant amount of time.

**Cons:**
- **More Complex Test Setup**: Generating unique names (e.g., appending a GUID or timestamp) makes the test setup more complex. You also need to handle these names consistently across the test (e.g., checking for uniqueness, saving the name, etc.).
- **Data Accumulation**: Even though you’re not deleting the user, creating users with unique names could eventually lead to **data accumulation** in the database. Over time, this could result in an increase in the volume of test data, which might need to be purged periodically.
- **Less Predictable**: It may make reading and debugging the tests harder, since the test data will change with every run. If you're trying to track specific user data (like verifying a fixed username), this approach will make it harder.

---

### **Which One, When?**

- If **data consistency** and **predictability** are more important (e.g., you need the same user to be present in all test environments), **deleting the user** after the test would be ideal. It ensures that no test leaves lingering data behind and that tests won’t interfere with each other. It also keeps the data in the database clean, so no old users accumulate.
  
- If **performance** and **test independence** are your primary goals (and you're willing to handle potential accumulation), then **making the username unique** would be a better approach. This ensures that each test runs without relying on cleanup steps, which can speed up larger test suites.

In many cases, combining **unique usernames** with a **cleanup strategy** (i.e., periodically purging old data) is a balanced approach.

---

## Updating Our Test

```c#
[Test]
public async Task GetUser_ShouldReturnUser_WithUniqueNameAndCleanup()
{
    // Arrange: Create a new user with a unique name (using GUID)
    var uniqueName = "John Doe99_" + Guid.NewGuid();
    var newUser = new User
    {
        Name = uniqueName,
        Password = "SecurePass123"
    };

    // Create the user (POST /api/Users)
    var createResponse = await _client.PostAsJsonAsync("api/Users", newUser);
    createResponse.EnsureSuccessStatusCode();

    // Act: Get the ID of the newly created user
    var createdUser = await createResponse.Content.ReadFromJsonAsync<User>();

    // Get user by ID using the returned ID from creation
    var getResponse = await _client.GetAsync($"api/Users/{createdUser.Id}");
    getResponse.EnsureSuccessStatusCode();

    // Get the user details
    var user = await getResponse.Content.ReadFromJsonAsync<User>();

    // Assert: Ensure the returned user matches the expected data
    Assert.NotNull(user);
    Assert.AreEqual(createdUser.Id, user.Id); // Ensure the ID matches
    Assert.AreEqual(uniqueName, user.Name); // Ensure the name is unique (John Doe99_<Guid>)
    Assert.AreEqual("SecurePass123", user.Password); // Ensure the password is SecurePass123

    // Cleanup: Delete the user after the test
    var deleteResponse = await _client.DeleteAsync($"api/Users/{createdUser.Id}");
    deleteResponse.EnsureSuccessStatusCode();
}

```

#### Changes

1. Unique Username

    The username is now dynamically generated by appending a GUID to the name "John Doe99_", ensuring uniqueness for each test run, e.g. `"John Doe99_880d08a2-11e9-417a-81cd-c4b64ee8c39f"`.

2. Deleting the User

    After the test assertions are done, a DELETE request is sent to remove the user from the database using the user’s ID (DELETE `/api/Users/{id}`).

---

## All Done? Not quite, sorry

If an assertion fails, the `DELETE` cleanup operation won't be executed, which could leave users in the database. 

To deal with this situation, we can use a **`try/catch`** block to ensure that the cleanup occurs, regardless of whether the assertions pass or fail. 

Alternatively, we can leverage NUnit’s **[SetUp]** and **[TearDown]** attributes to automate setup and cleanup operations.

### **1. Using `try/catch` for Cleanup**

The idea here is to perform the cleanup in a **`finally`** block, so it happens regardless of whether an exception (e.g., assertion failure) occurs. This ensures that the user will be deleted, even if the test fails.

```csharp
[Test]
public async Task GetUser_ShouldReturnUser_WithUniqueNameAndCleanup()
{
    // Create a unique name for the new user
    var uniqueName = "John Doe99_" + Guid.NewGuid();
    var newUser = new User
    {
        Name = uniqueName,
        Password = "SecurePass123"
    };

    User createdUser = null;

    try
    {
        // Create the user (POST /api/Users)
        var createResponse = await _client.PostAsJsonAsync("api/Users", newUser);
        createResponse.EnsureSuccessStatusCode();

        // Get the ID of the newly created user
        createdUser = await createResponse.Content.ReadFromJsonAsync<User>();

        // Act: Get user by ID
        var getResponse = await _client.GetAsync($"api/Users/{createdUser.Id}");
        getResponse.EnsureSuccessStatusCode();

        // Get the user details
        var user = await getResponse.Content.ReadFromJsonAsync<User>();

        // Assert: Ensure the returned user matches the expected data
        Assert.NotNull(user);
        Assert.AreEqual(createdUser.Id, user.Id); // Ensure ID matches
        Assert.AreEqual(uniqueName, user.Name); // Ensure name is unique
        Assert.AreEqual("SecurePass123", user.Password); // Ensure password matches
    }
    finally
    {
        // Ensure the user is deleted after the test, even if assertions fail
        if (createdUser != null)
        {
            var deleteResponse = await _client.DeleteAsync($"api/Users/{createdUser.Id}");
            deleteResponse.EnsureSuccessStatusCode();
        }
    }
}
```
- **`try` block** The code in the `try` block contains the creation, retrieval, and assertion steps.
- **`finally` block** The cleanup code (deleting the user) is placed in the `finally` block. This ensures that no matter whether the assertions pass or fail, the cleanup will happen.

This approach guarantees that the user will always be deleted after the test runs, even if something goes wrong in the test, preventing leftover data in the database.

---

### **2. Using `[SetUp]` and `[TearDown]` for Automatic Setup and Cleanup**

An alternate and cleaner solution is to use NUnit’s **[SetUp]** and **[TearDown]** attributes to automatically handle the user creation and cleanup before and after each test.

- **[SetUp]** Code in this method runs **before** each test to set up the user.
- **[TearDown]** Code in this method runs **after** each test to clean up the user, whether the test passes or fails.

Okay, let's update our tests.
> :exclamation: Please not the `Order(x)` has been removed from teast `[Test]` attribute - make sure you do too.

### **Update Tests with [SetUp] and [TearDown]**:

We're going to add a `[Setup]` fixture at the top of the class, and a `[Teardown]` at the bottom, and update one test for now - `GetUser_ShouldReturnUser`.

```csharp
public class UserApiTests
{
    private User _createdUser;
    private User _createdUser;

    [SetUp]
    public async Task SetUp()
    {
        _client = new HttpClient { BaseAddress = new Uri("https://localhost:7098/api/Users/") };  //replace URL with whatever your URL is

        // Create a new user before each test
        var uniqueName = "John Doe99_" + Guid.NewGuid();
        var newUser = new User
        {
            Name = uniqueName,
            Password = "SecurePass123"
        };

        var createResponse = await _client.PostAsJsonAsync("", newUser);
        createResponse.EnsureSuccessStatusCode();

        _createdUser = await createResponse.Content.ReadFromJsonAsync<User>();
    }

    //... previous tests

    [Test]
    public async Task GetUser_ShouldReturnUser()
    {
        var response = await _client.GetAsync(_createdUser.Id.ToString());
        response.EnsureSuccessStatusCode();

        var user = await response.Content.ReadFromJsonAsync<User>();

        Assert.NotNull(user);
        Assert.That(user.Id, Is.EqualTo(_createdUser.Id));
        Assert.That(user.Name, Is.EqualTo(_createdUser.Name));
        Assert.That(user.Password, Is.EqualTo(_createdUser.Password));
    }

    //...other tests

    [TearDown]
    public async Task TearDown()
    {
        if (_createdUser != null)
        {
            var deleteResponse = await _client.DeleteAsync(_createdUser.Id.ToString());
            deleteResponse.EnsureSuccessStatusCode();
        }

        _client.Dispose();
    }
}
```

- **[SetUp]** Before each test, the `SetUp` method creates a new user with a unique name. This is stored in the `_createdUser` field so it can be accessed in the test method and deleted later.
- **Test** The test method uses the `_createdUser` to perform the `GET` request and assert that the user data is correct.
- **[TearDown]** After the test, the `TearDown` method ensures that the user is deleted. This cleanup happens whether the test passes or fails.

### **Pros of `[SetUp]` and `[TearDown]`**
- **Cleaner and more structured** Using these attributes keeps the test code clean, separating setup and cleanup from the test logic.
- **Automatic management** The test setup and teardown are automatically called by NUnit, making the code easier to maintain and less error-prone.
  
---

### Which to use?

It depends. But since we are using NUnit, NUnit's **`[SetUp]`** and **`[TearDown]`** provides a cleaner, more structured approach to handling test data setup and cleanup. This is the **recommended** way to handle test setup/cleanup in NUnit, as it simplifies the test code and ensures consistency.

> :exclamation: HOWEVER! BEWARE trying to cram everything into your `[Setup]` or `[Teardown]` methods. You must be *absolutely* sure that each test in the class *needs* that data.

---

## Exercise: Refactor Tests to Use `[SetUp]` and `[TearDown]`

We now have one test that uses `[Setup]` and `[Teardown]`. You can now use those in every test. Refactor it so your test utilise the `_createdUser`

What do you notice about your tests now?

---

[>> Patterns and Antipatterns](./patterns-and-antipatterns.md)
