# SportsBetting Backend API

Backend API for a sports betting platform, built with **C# and ASP.NET Core**, following layered architecture and clean backend practices commonly used in **iGaming / Fintech systems**.

This project is being developed as a **portfolio project**, with a strong focus on:
- Clean architecture
- Transactional integrity
- Real-world backend patterns

---

#  Architecture

The solution is structured into clear layers:

- **API** – ASP.NET Core Web API (Controllers, Filters, Startup)
- **Application** – Use cases, business logic, DTOs
- **Domain** – Core domain entities and contracts
- **Infrastructure** – EF Core, repositories, Unit of Work, database access
- **Shared** – Common communication models and exceptions

This separation reflects how real backend teams structure maintainable systems.

# Architecture Approach for the moment:

This project currently follows a **modular monolith** architecture.

The decision to start as a monolith was intentional, focusing on:
- Clear domain boundaries
- Transactional consistency
- Simpler deployment and debugging
- Easier reasoning about business rules

The internal structure (Domain, Application, Infrastructure) allows future extraction
of individual modules into independent microservices if scalability or organizational
needs require it.

---

##  Tech Stack

- **.NET 9 / C#**
- **ASP.NET Core Web API**
- **Entity Framework Core**
- **SQL Server**
- **Swagger / OpenAPI**
- **AutoMapper**
- **Dependency Injection**
- **Unit of Work pattern**

---

##Features (Work in Progress)

- User registration
- Layered architecture with DI
- Repository + Unit of Work
- Centralized exception handling
- Swagger documentation

More features will be added incrementally.

---

## ▶️ Running the project (Development)

Requirements:
- .NET SDK
- SQL Server (local or container)

Run the API:

```bash
dotnet run --project src/Backend/SportsBetting.API
