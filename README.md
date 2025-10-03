you should add 

(.env)

# Database Configuration
ConnectionStrings__DefaultConnection=Server=db;Database=yourDB;User Id=sa;Password=Your_password;TrustServerCertificate=True;

# JWT Configuration
JWT__Key=
JWT__Issuer=http://localhost:5000/
JWT__Audience=
JWT__AccessTokenExpirationMinutes=15
JWT__RefreshTokenExpirationDays=7

# SMTP Configuration
SMTP__Host=smtp.gmail.com
SMTP__Port=587
SMTP__Email=yourgmail@gmail.com
SMTP__Password=

# Google OAuth Configuration
Authentication__Google__ClientId=
Authentication__Google__ClientSecret=

# Firebase Configuration
FCM__ProjectId=
FIREBASE_CREDENTIALS_BASE64=

# Application Configuration
BaseURL=http://localhost:5000/
Frontend__BaseUrl=http://127.0.0.1:5500
ASPNETCORE_URLS=http://+:8080


---------------------------------

(appsettings.json)
{
    "ConnectionStrings": {
        "DefaultConnection": "Server=.;Database=yourDB;Trusted_Connection=True;TrustServerCertificate=True"
    },
    "JWT": {
        "Key": "",
        "Issuer": "http://localhost:5000/",
        "Audience": "",
        "AccessTokenExpirationMinutes": 15,
        "RefreshTokenExpirationDays": 7
    },
    "Smtp": {
        "Host": "smtp.gmail.com",
        "Port": 587,
        "Email": "yourgmail@gmail.com",
        "Password": ""
    },
    "Authentication": {
        "Google": {
            "ClientId": "",
            "ClientSecret": ""
        }
    },
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
        }
    },
    "FCM": {
        "CredentialsPath": "firebase-credentials.json",
        "ProjectId": ""
    },
    "AllowedHosts": "*",
    "BaseURL": "http://localhost:5105/",
    "Frontend": {
        "BaseUrl": "http://127.0.0.1:5500"
    }
}


---------------

=>clone repo then put .env file in project then do docker compose up


