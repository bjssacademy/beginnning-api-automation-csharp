# **Examining the API Contract – Understanding the Structure of a Third-Party API**  

Before we can effectively mock a third-party API, we need to understand its structure. This means reviewing the API contract, which defines the endpoints, request formats, response structures, and possible error conditions.  

In this chapter, we’ll examine an imaginary API contract for a **payment provider** called **PayFast**. This will be the basis for our mock implementation and testing later on. 

---

## **Understanding an API Contract**  

An API contract is *usually* provided in one of the following formats:  
- **API Documentation** (e.g., OpenAPI/Swagger, Postman collections, or provider docs).  
- **Schema Definition** (e.g., JSON Schema, Protobuf, or XML WSDL).  
- **Example Requests and Responses** (often included in documentation in a wiki or hidden in Sharepoint).  

Our **PayFast API** follows a **RESTful** approach and provides endpoints for processing payments and retrieving transaction details.  

---

## **The PayFast API Contract**  

Because we're nice here, let's assume that you have been given not only the OpenAPI schema, but there's also a lot of useful, up-to-date information on the contract and how to use the endpoint. Lucky you.

### **1. Payment Processing Endpoint**  
This endpoint allows a user to initiate a payment.  

- **Endpoint:** `POST /api/payments`  
- **Headers:**  
  - `Authorization: Bearer {token}`  
  - `Content-Type: application/json`  
- **Request Body (JSON):**  

  ```json
  {
    "amount": 100.00,
    "currency": "GBP",
    "paymentMethod": "credit_card",
    "cardNumber": "4111111111111111",
    "expiryMonth": "12",
    "expiryYear": "2026",
    "cvv": "123"
  }
  ```
  
- **Success Response (200 OK):**  

  ```json
  {
    "transactionId": "abc123",
    "status": "approved",
    "amount": 100.00,
    "currency": "GBP",
    "timestamp": "2025-02-14T12:00:00Z"
  }
  ```

- **Failure Response (400 Bad Request) – Invalid Card:**  

  ```json
  {
    "error": "Invalid card details"
  }
  ```

- **Failure Response (402 Payment Required) – Insufficient Funds:**  

  ```json
  {
    "error": "Insufficient funds"
  }
  ```

---

### **2. Transaction Status Endpoint**  
This endpoint retrieves details about a previously processed payment.  

- **Endpoint:** `GET /api/payments/{transactionId}`  
- **Headers:**  
  - `Authorization: Bearer {token}`  
- **Success Response (200 OK):**  

  ```json
  {
    "transactionId": "abc123",
    "status": "approved",
    "amount": 100.00,
    "currency": "GBP",
    "timestamp": "2025-02-14T12:00:00Z"
  }
  ```

- **Failure Response (404 Not Found) – Invalid Transaction ID:**  

  ```json
  {
    "error": "Transaction not found"
  }
  ```

---

### **Key Observations**  

- The API uses **Bearer token authentication** (we may need to mock authentication too).  
- It has clear **status codes** (200 for success, 400 for invalid input, 402 for payment failure).  
- Responses return **consistent JSON structures**, making them easy to mock.  
- The `transactionId` is crucial for retrieving past transactions.  

---

Now that we understand the API’s structure, we can proceed with **creating a mock implementation**. This mock will allow us to simulate the behaviour of the PayFast API, enabling us to test our payment integration **without relying on a live system**.  

In the next section, we’ll build our mock API and make sure it mimics the real PayFast API contract.