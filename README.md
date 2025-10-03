For Backend Developer (with code):
```
git clone <your-repo>
cd <your-repo>
# Add .env file
docker-compose up -d
```

For Frontend Developer (without code):
```
#1. database
docker run -d --name ironer-sql -e "SA_PASSWORD=Your_password123" -e "ACCEPT_EULA=Y" -e "MSSQL_PID=Express" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest

#2. Add .env file in folder

#3. API ,open termenal cmd on env folder
docker run -d -p 5000:8080 --env-file .env --name ironer-api gaberbadr1/ironer-api:latest

```

.env(delete one of Database Configuration)
```
#1.Database Configuration (for backend)
ConnectionStrings__DefaultConnection=Server=db;Database=yourDB;User Id=sa;Password=Your_password;TrustServerCertificate=True;

#2.Database Configuration (for frontend)
#ConnectionStrings__DefaultConnection=Server=host.docker.internal,1433;Database=yourDB;User Id=sa;Password=Your_password;TrustServerCertificate=True;

# JWT Configuration
JWT__Key=ffgfsgegfgfgfgfgffggfggf
JWT__Issuer=http://localhost:5000/
JWT__Audience=test
JWT__AccessTokenExpirationMinutes=15
JWT__RefreshTokenExpirationDays=7

# SMTP Configuration
SMTP__Host=smtp.gmail.com
SMTP__Port=587
SMTP__Email=yourgamil@gmail.com
SMTP__Password=

# Google OAuth Configuration
Authentication__Google__ClientId=
Authentication__Google__ClientSecret=

# Firebase Configuration
FCM__ProjectId=
FIREBASE_CREDENTIALS_BASE64=fgj7u/ew0KICA.....

# Application Configuration
BaseURL=http://localhost:5000/
Frontend__BaseUrl=http://127.0.0.1:5500
ASPNETCORE_URLS=http://+:8080
```



---------------------------------

(appsettings.json) if needed
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


