# Authentication Setup Documentation

## Overview
ASP.NET Identity has been successfully integrated into the ScoreBurrow web application with the following features:
- User registration and login
- Password-based authentication
- Welcome message displaying the logged-in username
- Anonymous access allowed (site remains accessible without login)

## Infrastructure Changes

### Database Configuration
- **Database Name**: `ScoreBurrowDb` (configurable via infrastructure parameters)
- **Connection String**: Configured via App Service app settings
- **Authentication Method**: Azure AD Managed Identity for Azure SQL

The infrastructure now passes the SQL connection string to the App Service through app settings:
- `infrastructure/main.bicep`: Added `appServiceConfig` resource to configure connection string
- `infrastructure/modules/appService.bicep`: Updated to support app settings configuration

## Application Changes

### NuGet Packages Added
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` v8.0.0
- `Microsoft.EntityFrameworkCore.SqlServer` v8.0.0
- `Microsoft.EntityFrameworkCore.Tools` v8.0.0

### New Files Created
1. **Models**
   - `Models/ApplicationUser.cs` - Custom Identity user model

2. **Data**
   - `Data/ApplicationDbContext.cs` - Identity database context

3. **Pages**
   - `Pages/Account/Login.razor` - Login page with email/password form
   - `Pages/Account/Register.razor` - User registration page
   - `Pages/Account/Logout.razor` - Logout handler

4. **Migrations**
   - `Migrations/[timestamp]_InitialIdentity.cs` - EF Core migration for Identity tables

### Modified Files
1. **Program.cs**
   - Added DbContext configuration with SQL Server
   - Added Identity services with password requirements
   - Added authentication and authorization middleware

2. **Shared/MainLayout.razor**
   - Added `AuthorizeView` component
   - Shows "Welcome, [Username]" for authenticated users
   - Shows Login/Register links for anonymous users

3. **appsettings.json**
   - Added `ConnectionStrings` section with LocalDB for development

## Deployment Instructions

### 1. Deploy Infrastructure
The infrastructure changes are ready to deploy. When you deploy using the Bicep templates:
```bash
cd infrastructure
./deploy.sh
```

The infrastructure will:
- Create Azure SQL Server and Database
- Configure App Service with the SQL connection string
- Set up Managed Identity authentication

### 2. Deploy Application
Deploy the application to Azure App Service:
```bash
./deploy-app.sh
```

### 3. Apply Database Migrations
After the first deployment, the database schema needs to be created. You have two options:

**Option A: Automatic (Recommended for development)**
The application can automatically apply migrations on startup by adding this code to `Program.cs`:
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}
```

**Option B: Manual (Recommended for production)**
Apply migrations manually using EF Core tools:
```bash
export PATH="$PATH:/Users/nikitasmelov/.dotnet/tools"
cd src/ScoreBurrow.Web
dotnet ef database update --connection "YOUR_AZURE_SQL_CONNECTION_STRING"
```

## Local Development

For local development on macOS (LocalDB not supported):
1. Use Azure SQL Database for development
2. Or use Docker to run SQL Server locally:
   ```bash
   docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Password" \
      -p 1433:1433 --name sql1 -d mcr.microsoft.com/mssql/server:2022-latest
   ```
3. Update `appsettings.Development.json` with the appropriate connection string

## Password Requirements
Default password requirements:
- Minimum 6 characters
- Requires digit
- Requires lowercase letter
- Requires uppercase letter
- Non-alphanumeric characters NOT required

## Features

### Anonymous Access
The site remains fully accessible to anonymous users. Authentication is optional.

### User Experience
- **Anonymous users** see Login and Register links in the top navigation
- **Authenticated users** see "Welcome, [email]" and a Logout link
- Login page includes "Remember me" checkbox
- Registration page validates password confirmation
- Both pages include links to switch between login/register

## Security Considerations
1. HTTPS is enforced (configured in infrastructure)
2. Passwords are hashed using ASP.NET Identity defaults
3. Authentication cookies are secure
4. SQL connection uses Managed Identity in Azure (no passwords in config)

## Next Steps
1. Customize `ApplicationUser` model to add custom properties (e.g., DisplayName, FirstName, LastName)
2. Add email confirmation workflow
3. Implement password reset functionality
4. Add two-factor authentication
5. Customize the UI/styling of authentication pages
6. Add authorization policies and roles if needed
