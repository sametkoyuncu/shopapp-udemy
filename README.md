# ShopApp
## What is this project?
This project is simple shopping website with dotnet core mvc. Created watching this [Udemy course](https://www.udemy.com/course/komple-web-developer-kursu/ "Udemy course"). 

Project have default two type user: Admin and Customer.
**Customer Users can use these:**
- register with mail confirm, forgot password and login
- look products, add to cart and delete than cart
- simple search on products
- pay with credit card (using iyzipay api)

**Admin Users can use CRUD operations.**

# How to run?
- install dotnet core 3.1 sdk --> https://dotnet.microsoft.com/download/dotnet-core/3.1
- open folder with your code ide for example 'vs code'
- open terminal screen
- change directory use this command --> `cd shopp.webui`
- and start your project with this command --> `dotnet run`

### How to use CreditCart Api?
- create account --> https://sandbox-merchant.iyzipay.com/auth
- open this file --> /shopapp.webui/controllers/cartcontroller.cs
- find `PaymentProcess` method

```csharp
private Payment PaymentProcess(OrderModel model)
        {
            Options options = new Options();
            options.ApiKey = "your iyzipay api key";
            options.SecretKey = "your iyzipay secret key";
		...
		}
```
- write your apikey and secretkey values

### How to use EmailSender?
- open this file --> /shopapp.webui/appsettings.json
- find `EmailSender`

```json
"EmailSender": {
    "Host":"your smtp host",
    "Port":123,
    "EnableSSL":true,
    "UserName":"your email address",
    "Password":"your email password"
  }
```
- write your smtp host, port, mail and password values

##  Login Infos

    Admin User
    --Email     :   adminuser@site.com
    --Password  :   Toor_1232

    Customer User
    --Email     :   kfk46667@eoopy.com
    --Password  :   Toor_1232