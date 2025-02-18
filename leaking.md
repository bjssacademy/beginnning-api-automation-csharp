# Leaking Information

Right now, our API is returning **sensitive user data**, including hashed passwords and salts. This means:  

- Attackers could **steal** hashed passwords if they gain access.  
- Even though they are hashed, common passwords could still be cracked (especially if salting is not used properly).  
- It violates **security best practices** — we should only expose what is necessary.  

Let's fix that.

## **Use DTOs to Control What Gets Exposed**  
Instead of returning the **entire User object**, we should create a **UserResponseDto** to control what data is exposed.

### **1. Define a Secure DTO**
We create a class that only contains the **safe** fields we want to return, `UserResponseDto.cs` in our `Model` folder.

```csharp
public class UserResponseDto
{
    public long Id { get; set; }
    public string Name { get; set; }
}
```

### **2. Update API Endpoints to Use DTOs**
Now, we modify our API methods to **return DTOs** instead of the full `User` model.

#### **Get All Users**
```csharp
[Authorize]
[HttpGet]
public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
{
    var users = await _context.Users.ToListAsync();

    return users.Select(u => new UserResponseDto
    {
        Id = u.Id,
        Name = u.Name
    }).ToList();
}
```
---

## Interlude: Welcome to LINQ

You may be wondering what the hell that `return users.Select...` bit of code is all about. I can best summarise it as:

"For each user in the `users` collection, create a new `UserResponseDto` object where `Id` and `Name` are copied from the original `user` object. After that, collect all the new `UserResponseDto` objects into a list and return it."

In a bit more detail:

1. The `users` Collection

    First, users a collection (like a list, array, or any enumerable) of user objects. Each user object might has properties like Id, Name, etc. The idea here is to transform the users collection into a new list where each item is a UserResponseDto.

2. The `Select` Method

    Select is a LINQ method that projects each element of the collection into a new form. Essentially, it allows you to transform each item from the original collection (  ) into something new. In this case, you’re transforming each user into a new     object.

    The parameter `u` represents each `user` in the `users` collection as it is iterated through.

    For each user, we're creating a new instance of `UserResponseDto` with `Id` and `Name` properties being populated with the corresponding properties from the `user`.

3. The Projection (`new UserResponseDto { ... }`)

    Inside the `Select`, we're creating a new object (`new UserResponseDto { ... }`). This is a way of shaping or transforming each `user` object into a new object of type `UserResponseDto` by selecting specific fields from the original `user` object.

    The `Id = u.Id` line copies the Id from the original `user` to the `UserResponseDto`.
    The `Name = u.Name` line does the same for the `Name`.
    
    This creates a new `UserResponseDto` for every `user` in the original `users` collection.

4. The `ToList` Method

    After applying `Select` to transform all the users, `ToList()` is called to convert the results from a sequence (an `IEnumerable<UserResponseDto>`) into a concrete `List<UserResponseDto>`. This is useful when we want to return a list of objects to work with later, or in situations where we need to ensure the result is stored in a list rather than a "lazy-evaluated" sequence.

---

#### **Get a Single User**
```csharp
[Authorize]
[HttpGet("{id}")]
public async Task<ActionResult<UserResponseDto>> GetUser(long id)
{
    var user = await _context.Users.FindAsync(id);

    if (user == null)
    {
        return NotFound();
    }

    return new UserResponseDto
    {
        Id = user.Id,
        Name = user.Name
    };
}
```

#### **Update a User**
For `PutUser`, we shouldn't take the whole `User` object as input. Instead, we define a **UpdateUserDto** to ensure that users can **only update specific fields** (like their name, but not their password directly for example).

```csharp
public class UserUpdateDto
{
    public long Id { get; set; }
    public string Name { get; set; }
}
```

Now, update the endpoint:

```csharp
[Authorize]
[HttpPut("{id}")]
public async Task<IActionResult> PutUser(long id, UpdateUserDto userUpdate)
{
    if (id != userUpdate.Id)
    {
        return BadRequest();
    }

    var user = await _context.Users.FindAsync(id);

    if (user == null)
    {
        return NotFound();
    }

    user.Name = userUpdate.Name; // Only update allowed fields

    _context.Entry(user).State = EntityState.Modified;

    try
    {
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        if (!UserExists(id))
        {
            return NotFound();
        }
        else
        {
            throw;
        }
    }

    return NoContent();
}
```

## **Key Takeaways**
1. **Never expose sensitive data**—even if it’s hashed!  
2. **Use DTOs** to control what gets returned.  
3. **Limit what users can update**—don’t allow raw `User` objects in API requests.  

---

[>> Contract Testing](./contract.md)