# Foods_Restaurant
This project for creating a restaurant management system to enable customers to order food online and get delivered for them. 
<br>
Foods Restaurant where yours would be able to place an order using a credit card and admin would manage order placed all the way till it is picked up.

# Features
- Full CURD Operations for admininstarator for contorl his system
- Build with asp.net core 3.1, EntityFramework core with code first approch, jQuery, Bootstrap 4, AJAX
- Including alot of functionalities Areas like Customer, Admin, Identity
- Using Third parts like Stripe payment gateway for credit cards and SendGrid gateway for sending emails
- Using very nice javascript plugins Like Datatable.js, timepicker.js, toastr, sweetalert.js, fontawesome

## How to run it
- Download and install [dotnet core 3.1](https://dotnet.microsoft.com/download/dotnet/3.1)
- Clone the repository
- Fill the following information with your custom account configurations for third party
  - In `Startup.cs` file
    - Put your Facebook app authentication keys in
      ```csharp
            services.AddAuthentication().AddFacebook(facebookOptions => 
            {
                facebookOptions.AppId = "xxxxxxxxx";
                facebookOptions.AppSecret = "xxxxxxxxxxxxxxxxx";
            });
      ```
    - Put your Google app authentication keys in
      ```csharp
            services.AddAuthentication().AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = "xxxxxxxxxxxxxxx";
                googleOptions.ClientSecret = "xxxxxxxxxxxxxxx";
            });
      ```
  - In `appsettings.json` file
     - Put your Stripe test or live mode secret keys
       ```json
         "Stripe": {
            "SecretKey": "sk_test_xxxxxxxxxxxxxxxxxxxxxxxxx",
            "PublishableKey": "pk_test_xxxxxxxxxxxxxxxxxxx"
          },
       ```
     - Put your SendGride App key for sending emails
       ```json
         "SendGridKey": "xxxxxxxxxxxxxxxxxxxxxxxx"
       ```
- Ensure that you download and install SQL Server, in addition for the correct `ConnectionStrings` section in `appsettings.json` file
- Migrate the database with entityframework and seed data by running the following command in direcotry repository
  ```shell
   $ dotnet ef database update 
  ```
- in the repository directory run this code open terminal/cmd and run this command
   ```shell
   $ dotnet build
   $ dotnet run
  ```

# Demo
- For testing, you can login any role (blews) you need, they have the same (Password: Admin123):
    - Manager User:        admin@gmail.com
    - Kitchen User:        kitchen@gmail.com
    - Front Desk User:     front@gmail.com
- Demo Link => https://f00ds.herokuapp.com or http://foods.somee.com
    
- In order to place order you can use any test credit card number supported by stripe.
    A default example is 4242 4242 4242 4242, any valid date , any 3 digit CVV.
