# **Salting – Strengthening Password Security**  

In the previous section, we introduced **hashing** to avoid storing passwords in plain text. But hashing alone isn’t enough. Attackers can still use **rainbow tables**, which are precomputed lists of common password hashes, to crack weak passwords.  

**And the solution?** **Salting!**.  

---

## **What's Salting?**  

Salting means adding a **random value** (the "salt") to the password **before hashing** it. This ensures that even if two users have the same password, they get **different hashes**.

### **Example Without Salting (Weak)**
| Password  | Hash |
|-----------|---------------------------|
| `Password123` | `3a1f...9b8c` |
| `Password123` | `3a1f...9b8c` |

Hashes are **identical**, so an attacker can guess common passwords.

---

### **Example With Salting (Stronger)**  

| Password  | Salt  | Hash |
|-----------|-------|---------------------------|
| `Password123` | `xyz123` | `7f3d...8a2b` |
| `Password123` | `abc456` | `a9b2...4e6f` |

Even though both users picked `"Password123"`, their final stored hashes **are different** because of unique salts.

---

## **Implementing Salting**  

We'll modify our `PasswordHasher` class to include a randomly generated salt.

### **1. Update Password Hashing**
Modify `PasswordHasher.cs` to generate a salt before hashing.

```csharp
using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

public static class PasswordHasher
{
    public static string HashPassword(string password, out string salt)
    {
        // Generate a random salt (16 bytes)
        byte[] saltBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        salt = Convert.ToBase64String(saltBytes);

        // Hash the password with the salt
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: saltBytes,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 32));

        return hashed;
    }

    public static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
    {
        byte[] saltBytes = Convert.FromBase64String(storedSalt);

        // Hash the entered password using the stored salt
        string enteredHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: enteredPassword,
            salt: saltBytes,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 32));

        // Compare hashes
        return enteredHash == storedHash;
    }
}
```

- **`out string salt`** ensures we return the salt alongside the hash.
- **PBKDF2 with HMACSHA256** is a strong hashing algorithm.
- **Random salt ensures unique hashes**, even if passwords are the same.

---

### **2️. Update the User Model**  

Now that we’re storing a salt, update `User.cs`:  

```csharp
namespace UserAuthAPI.Models
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string HashedPassword { get; set; }
        public string Salt { get; set; } // New field for salting
    }
}
```

---

### **3️. Update User Registration to Store Salt**  

Modify `PostUser` in `UsersController.cs`:

```csharp
[HttpPost]
public async Task<ActionResult<User>> PostUser(UserDto user)
{
    if (string.IsNullOrWhiteSpace(user.Name) || string.IsNullOrWhiteSpace(user.Password))
    {
        return BadRequest(new { message = "Name and Password cannot be empty." });
    }

    // Hash password with a generated salt
    string hashedPassword = PasswordHasher.HashPassword(user.Password, out string salt);

    var newUser = new User
    {
        Name = user.Name,
        HashedPassword = hashedPassword,
        Salt = salt // Store salt in the database
    };

    _context.Users.Add(newUser);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, new { id = newUser.Id, name = newUser.Name });
}
```

---

### **4. Update Login to Verify Passwords Correctly**  

Modify `LoginUser` to check passwords using the stored salt.

> This is a small change, we only need to update the line `bool isPasswordValid = PasswordHasher.VerifyPassword(userDto.Password, user.HashedPassword, user.Salt);`
>
> However, the complete code is below:

```csharp
[AllowAnonymous]
[HttpPost("login")]
public async Task<IActionResult> LoginUser(UserDto userDto)
{
    if (string.IsNullOrWhiteSpace(userDto.Name) || string.IsNullOrWhiteSpace(userDto.Password))
    {
        return BadRequest(new { message = "Name and/or Password cannot be empty." });
    }

    // Find user by name
    var user = _context.Users.SingleOrDefault(x => x.Name == userDto.Name);
    if (user == null)
    {
        return Unauthorized(new { message = "Invalid credentials" });
    }

    // Verify hashed password
    bool isPasswordValid = PasswordHasher.VerifyPassword(userDto.Password, user.HashedPassword, user.Salt);
    if (!isPasswordValid)
    {
        return Unauthorized(new { message = "Invalid credentials" });
    }

    var token = GenerateJwtToken(user);

    return Ok(new { token });
}
```

Now, instead of directly comparing passwords, we **re-hash the entered password with the stored salt** and check if the result matches the stored hash.

---

## **Key Security Pointers**  

1. **Prevent Rainbow Table Attacks** – Since each password gets a unique salt, precomputed hash databases become useless.  
2. **Prevent Identical Hashes** – Even if two users have the same password, their stored hashes will be different.  
3. **Industry Standard** – This is the method used by secure authentication systems.

---



[>> Leaking Information](./leaking.md)
