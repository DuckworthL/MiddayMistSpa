# MiddayMist Spa & Wellness - Management System

A full-stack spa and wellness business management system built with .NET 8, Blazor Server, and ASP.NET Core Web API. Designed for Philippine business compliance with integrated HR, payroll, accounting, and customer management.

---

## Features

- **Appointment Scheduling** — Book, reschedule, cancel, and track service appointments with room and therapist assignment
- **Customer Management** — Customer profiles, membership tiers, loyalty points, visit tracking, and segmentation analytics
- **Employee Management** — Staff profiles, positions, commission tracking, and performance reporting
- **Shift Management** — Weekly schedules, shift exceptions, time-off requests, and attendance tracking
- **Point of Sale** — Service and product transactions, multi-currency support (via Frankfurter API), discounts, and refunds
- **Inventory Management** — Product stock tracking, purchase orders, suppliers, low-stock/out-of-stock alerts, batch management
- **Payroll** — Semi-monthly payroll with Philippine statutory deductions (SSS, PhilHealth, Pag-IBIG, withholding tax), bonuses, and payslip generation
- **Accounting** — Double-entry bookkeeping, chart of accounts, journal entries, and financial summaries
- **Reports & Analytics** — Revenue trends (daily/weekly/monthly), staff performance, customer insights, exportable reports (PDF, Excel, CSV)
- **Notifications** — Real-time alerts for appointments, low stock, payroll, and system events
- **Security** — JWT authentication, two-factor authentication (TOTP), role-based access control, session management, audit logging
- **Multi-Currency POS** — Live exchange rates with automatic refresh

---

## Tech Stack

| Layer             | Technology                                                     |
| ----------------- | -------------------------------------------------------------- |
| **Frontend**      | Blazor Server (.NET 8), Bootstrap 5, Bootstrap Icons, Chart.js |
| **Backend API**   | ASP.NET Core 8 Web API                                         |
| **Database**      | SQL Server (LocalDB for dev, SQL Server for production)        |
| **ORM**           | Entity Framework Core 8                                        |
| **Auth**          | JWT Bearer tokens, TOTP-based 2FA (OTP.NET + QRCoder)          |
| **Exports**       | QuestPDF (PDF), ClosedXML (Excel), CSV                         |
| **External APIs** | Frankfurter (exchange rates), ipwhois (geolocation)            |

---

## Project Structure

```
MiddayMistSpa/
├── src/
│   ├── MiddayMistSpa.Core/           # Domain entities, interfaces, constants
│   ├── MiddayMistSpa.Infrastructure/  # EF Core DbContext, migrations, repositories
│   ├── MiddayMistSpa.API/            # Web API controllers, services, DTOs
│   └── MiddayMistSpa.Web/            # Blazor Server frontend
├── Database.txt                       # Full SQL schema reference
├── DEPLOYMENT.md                      # MonsterASP.NET deployment guide
└── MiddayMistSpa.slnx                # Solution file
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server LocalDB (included with Visual Studio) or SQL Server instance

### Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/DuckworthL/MiddayMistSpa.git
   cd MiddayMistSpa
   ```

2. **Restore packages**

   ```bash
   dotnet restore
   ```

3. **Run the API** (terminal 1)

   ```bash
   cd src/MiddayMistSpa.API
   dotnet run
   ```

   The API starts at `http://localhost:5286`. On first run, the database is automatically created and seeded with sample data.

4. **Run the Web app** (terminal 2)

   ```bash
   cd src/MiddayMistSpa.Web
   dotnet run
   ```

   The web app starts at `http://localhost:5004`.

5. **Login**
   - Username: `superadmin`
   - Password: `SuperAdmin@2026!`

---

## Default Credentials

| Role                 | Username     | Password           |
| -------------------- | ------------ | ------------------ |
| System Administrator | `superadmin` | `SuperAdmin@2026!` |

Additional staff accounts are seeded automatically — check the Employees and User Management pages after login.

---

## Deployment

See [DEPLOYMENT.md](DEPLOYMENT.md) for step-by-step instructions on deploying to MonsterASP.NET or any .NET 8 compatible host.

---

## License

This project is for educational and demonstration purposes.
