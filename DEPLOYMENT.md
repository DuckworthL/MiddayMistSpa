# MiddayMist Spa - MonsterASP.NET Deployment Guide

## Prerequisites

- MonsterASP.NET hosting account with .NET 8.0 support
- SQL Server database access
- FTP/SFTP client (FileZilla, WinSCP, etc.)

---

## Step 1: Create Database on MonsterASP.NET

1. Log in to your MonsterASP.NET control panel
2. Navigate to **Databases** → **MS SQL**
3. Click **Create Database**
4. Note down:
   - Database Name: `your_database_name`
   - Server: `sql.monsterasp.net` (or as shown in control panel)
   - Username: (usually auto-generated)
   - Password: (set a strong password)

---

## Step 2: Update Production Configuration

Edit `src/MiddayMistSpa.Web/appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql.monsterasp.net;Database=YOUR_DATABASE_NAME;User Id=YOUR_DB_USERNAME;Password=YOUR_DB_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  },
  "ApiSettings": {
    "BaseUrl": "https://middaymistspa.monsterasp.net"
  },
  "JwtSettings": {
    "SecretKey": "YOUR_SUPER_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG!"
  }
}
```

**Important**: Change the `SecretKey` to a unique, secure value (at least 32 characters)!

---

## Step 3: Publish the Application

### Option A: Using Command Line (Recommended)

```powershell
cd c:\Users\golia\MiddayMistSpa\src\MiddayMistSpa.Web

# Publish for production
dotnet publish -c Release -o ./bin/Publish/MonsterASP
```

### Option B: Using Visual Studio

1. Right-click on `MiddayMistSpa.Web` project
2. Select **Publish**
3. Choose **Folder** profile
4. Select the `MonsterASP` publish profile
5. Click **Publish**

---

## Step 4: Upload Files to MonsterASP.NET

1. Connect via FTP to your MonsterASP.NET account
   - Host: `ftp.monsterasp.net` (or as shown in control panel)
   - Username: Your FTP username
   - Password: Your FTP password
   - Port: 21 (or 22 for SFTP)

2. Navigate to your site's root directory (usually `/site/wwwroot/`)

3. Upload ALL files from:

   ```
   src\MiddayMistSpa.Web\bin\Publish\MonsterASP\
   ```

4. Ensure the following are uploaded:
   - `MiddayMistSpa.Web.dll` (main app)
   - `MiddayMistSpa.API.dll` (embedded API)
   - `appsettings.Production.json` (your configured settings)
   - `web.config` (IIS configuration)
   - `wwwroot/` folder (static files)

---

## Step 5: Configure IIS Application Pool

In MonsterASP.NET control panel:

1. Go to **IIS Manager** or **Application Settings**
2. Ensure the application pool uses:
   - **.NET CLR Version**: No Managed Code
   - **Integrated Pipeline Mode**: Integrated
3. Set the application to use **.NET 8.0**

---

## Step 6: First-Time Database Setup

The application will automatically:

1. Create all database tables on first run
2. Seed initial data (admin users, sample services)

### Default Login Credentials:

| Username     | Password           | Role       |
| ------------ | ------------------ | ---------- |
| `superadmin` | `SuperAdmin@2026!` | SuperAdmin |
| `admin`      | `Admin@2026!`      | Admin      |

**Important**: Change these passwords immediately after first login!

---

## Step 7: Verify Deployment

1. Navigate to `https://middaymistspa.monsterasp.net`
2. You should see the login page
3. Log in with `superadmin` / `SuperAdmin@2026!`
4. Test the API health: `https://middaymistspa.monsterasp.net/health`

---

## Troubleshooting

### Error: 500 Internal Server Error

- Check `web.config` is properly uploaded
- Enable logging in `web.config`:
  ```xml
  <aspNetCore ... stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout">
  ```
- Check the `logs` folder for error details

### Error: Database Connection Failed

- Verify connection string in `appsettings.Production.json`
- Test database connectivity from MonsterASP.NET control panel
- Ensure database user has proper permissions

### Error: 404 on API Endpoints

- Ensure all DLL files are uploaded
- Check that `MiddayMistSpa.API.dll` is present
- Verify `web.config` handlers are configured

### SignalR/Blazor Connection Issues

- Ensure WebSockets is enabled in your MonsterASP.NET plan
- Check if any firewall/proxy is blocking WebSocket connections

---

## File Structure After Deploy

```
/site/wwwroot/
├── MiddayMistSpa.Web.dll          # Main Blazor Server app
├── MiddayMistSpa.API.dll          # Embedded API controllers
├── MiddayMistSpa.Core.dll         # Core entities
├── MiddayMistSpa.Infrastructure.dll # Data layer
├── appsettings.json               # Base settings
├── appsettings.Production.json    # Production overrides
├── web.config                     # IIS configuration
├── wwwroot/                       # Static files
│   ├── app.css
│   ├── lib/bootstrap/
│   └── ...
└── logs/                          # Log files (created automatically)
```

---

## Security Checklist

- [ ] Changed default admin passwords
- [ ] Changed JWT secret key in production settings
- [ ] Enabled HTTPS (automatic on MonsterASP.NET)
- [ ] Removed or secured development endpoints
- [ ] Verified database connection is encrypted (TrustServerCertificate)

---

## Support

For MonsterASP.NET specific issues:

- MonsterASP.NET Support: https://www.monsterasp.net/support
- Knowledge Base: https://www.monsterasp.net/kb

For application issues:

- Check the logs folder for detailed error messages
- Review the API health endpoint: `/health`
