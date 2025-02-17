# Test Doubles: Mocks, Stubs, Fakes, Spies

Is it a mock? Or is it a stub? Or a fake? These terms are often used interchangeably, but they have distinct meanings in testing.  

## **1. Test Double (Umbrella Term)**  
A **test double** is a general term for any object that replaces a real component in a test. Mocks, stubs, fakes, and spies all fall under this category.

---

## **2. Stub**  
A **stub** is a simple, pre-programmed object that returns fixed responses to method calls. It doesn’t have any logic beyond returning hardcoded data.  

**Use when...** you need a consistent, predefined response (e.g., always returning `"approved"` for a payment request).  

**Example:**  
```csharp
public class PaymentServiceStub : IPaymentService
{
    public PaymentResponse ProcessPayment(PaymentRequest request)
    {
        return new PaymentResponse { TransactionId = "12345", Status = "approved" };
    }
}
```
- **Does not verify interactions**—it only provides fixed data.  

> :exclamation: At the moment, we have a stub, but we've called it a mock. Confusing, right?

---

## **3. Fake**  
A **fake** is a lightweight implementation of a real system that behaves like the real thing but is simplified. Unlike a stub, it can have in-memory logic.  

**Use when...** you need a working system without setting up external dependencies (e.g., an in-memory database instead of a real one).  

**Example:**  
```csharp
public class FakePaymentRepository : IPaymentRepository
{
    private readonly Dictionary<string, PaymentResponse> _data = new();

    public void Save(PaymentResponse payment) => _data[payment.TransactionId] = payment;

    public PaymentResponse Get(string transactionId) => _data.TryGetValue(transactionId, out var response) ? response : null;
}
```
- **Has basic internal logic** but doesn’t persist data permanently.  

---

## **4. Mock**  
A **mock** is a test double that verifies **interactions**. It checks **how** the system used it, such as whether a method was called with specific parameters.  

**Use when...** you need to ensure a method was called the correct number of times or with the right parameters.  

**Example (using Moq):**  
```csharp
var mockService = new Mock<IPaymentService>();

mockService.Setup(p => p.ProcessPayment(It.IsAny<PaymentRequest>()))
    .Returns(new PaymentResponse { TransactionId = "mock123", Status = "approved" });

mockService.Verify(p => p.ProcessPayment(It.IsAny<PaymentRequest>()), Times.Once);
```
- **Mocks track method calls** and assert on them later.  

---

## **5. Spy**  
A **spy** is like a mock but keeps track of method calls without requiring pre-configuration. It’s used when you want to verify interactions but still use a real implementation.  

**Use when...** testing behaviour but still needing some real logic.  

**Example:**  
```csharp
public class PaymentServiceSpy : IPaymentService
{
    public int CallCount { get; private set; }

    public PaymentResponse ProcessPayment(PaymentRequest request)
    {
        CallCount++;
        return new PaymentResponse { TransactionId = "spy123", Status = "approved" };
    }
}
```
- **Can be used in assertions:**  
```csharp
var spy = new PaymentServiceSpy();
spy.ProcessPayment(new PaymentRequest());
Assert.That(spy.CallCount, Is.EqualTo(1));
```

---

## **Summary Table**
| Type   | Returns Data? | Has Logic? | Tracks Calls? | Used For |
|--------|-------------|------------|---------------|------------|
| **Stub** | ✅ Fixed Data | ❌ No Logic | ❌ No Tracking | Simple predefined responses |
| **Fake** | ✅ Realistic Data | ✅ Some Logic | ❌ No Tracking | In-memory database, lightweight implementation |
| **Mock** | ✅ Predefined | ❌ No Logic | ✅ Tracks Calls | Verifying method interactions |
| **Spy** | ✅ Real Implementation | ✅ Some Logic | ✅ Tracks Calls | Testing method call frequency |
| **Test Double** | ✅ Varies | ✅ Varies | ✅ Varies | General category for all of these |

---

## **Which One Should You Use?**  
- **Stub**: When you only care about return values (e.g., “always return `200 OK`”).  
- **Fake**: When you need a lightweight alternative to an external dependency (e.g., an in-memory DB).  
- **Mock**: When verifying that a method was called with the right parameters.  
- **Spy**: When using a real implementation but also need to check how many times it was used.  

For our **mock payment API**, we’re using a **stub** (since we return predefined responses), but later we'll turn it into a **mock**.  

---

## **Switching Between Mock and Live API**  

Right now, our tests use the mock payment API running on `localhost:8080`. However, in a real environment, we’d want to send requests to the actual payment provider’s API. The best way to manage this is through **environment configuration**, allowing us to switch between the mock and live API dynamically.  

Since we don’t have an `appsettings.json` file in this test setup, we’ll use a **`.env` file with dotenv**. This keeps our API URL configurable while keeping secrets out of the codebase.  

---

## **Step 1: Install dotenv.net**  

1. Open **Visual Studio**.  
2. In **Solution Explorer**, right-click on your **test project** and select **Manage NuGet Packages**.  
3. Go to the **Browse** tab and search for **dotenv.net**.  
4. Select the package and click **Install**.  
5. Accept any license agreements if prompted.  

Now, we can use **dotenv.net** to load configuration values from a `.env` file.  

---

## **Step 2: Create a `.env` File**  

Inside your test project’s **root directory**, create a new file named **`.env`** and add the following contents:  

```plaintext
PAYMENT_API_URL=http://localhost:8080/
```

Later, when we want to test against the real API, we can change this value to:  

```plaintext
PAYMENT_API_URL=https://realpaymentprovider.com/
```

---

## **Step 3: Load Environment Variables in Tests**  

We need to modify our test setup so that it dynamically picks the API URL from the `.env` file. Update your **test setup (`SetUp` method in NUnit)** to load the environment variable:

```csharp
using dotenv.net;

[OneTimeSetUp]
public void OneTimeSetUp()
{
    DotEnv.Load();
    var baseUrl = Environment.GetEnvironmentVariable("PAYMENT_API_URL")
                  ?? throw new Exception("PAYMENT_API_URL not set");
    _mockServer = new PaymentApiMock();
    _mockServer.Start();
    _client = new RestClient(baseUrl);
}
```

### **Explanation:**  
- `DotEnv.Load();` → Reads the `.env` file and loads variables into the environment.  
- `Environment.GetEnvironmentVariable("PAYMENT_API_URL")` → Retrieves the API URL from the `.env` file.  
- If the variable isn’t set, an exception is thrown to prevent silent failures.  
- `_client = new RestClient(baseUrl);` → Configures RestSharp to use the correct API URL.  

---

## **Step 4: Run our Tests**  

All your tests should pass as before!

### **What Does This Achieve?**  
- Our tests will now **automatically switch** between the mock and live API by simply changing the `.env` file.  
- There’s **no hardcoded API URL** in our test code, reducing duplication and increasing maintainability.  
- We **avoid accidental live API calls** during local testing.  

---

[Stub to Mock](./stubtomock.md)