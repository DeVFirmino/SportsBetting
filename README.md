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

Layered Architecture based on **Clean Architecture principles**

The decision to start as a monolith was intentional, focusing on:

- Clear domain boundaries
- Transactional consistency
- Simpler deployment and debugging
- Easier reasoning about business rules
 

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

## Features (Work in Progress)

- User registration
- Layered architecture with DI
- Repository + Unit of Work
- Centralized exception handling
- Swagger documentation

More features will be added incrementally.

---

## Code Quality

This project uses **Qodana by JetBrains Rider** for static code analysis.

Qodana helps ensure:
- Consistent coding standards
- Early detection of potential bugs
- Maintainable and readable codebase

The analysis configuration is versioned in the repository and can be executed
locally via JetBrains Rider or through CI workflows.

This reflects a real-world development practice commonly used in professional
.NET teams.

## Running the project (Development)

Requirements:
- .NET SDK
- SQL Server (local or container)

---

Run the API:

```bash
dotnet run --project src/Backend/SportsBetting.API
