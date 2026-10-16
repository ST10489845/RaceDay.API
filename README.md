# RaceDay Event Management System — API

[![.NET CI](https://github.com/STM10489845/RaceDay.API/actions/workflows/ci.yml/badge.svg)](https://github.com/STM10489845/RaceDay.API/actions/workflows/ci.yml)

A RESTful API built with **ASP.NET Core Web API (.NET 8)** that powers the RaceDay event management platform for the South African road running, walking, and cycling community. This is **Part 2** of the RaceDay system.

---

## 📌 Project Overview

The RaceDay API enables:

- **Organisers** to create and manage events, define categories, and capture participant results.
- **Participants** to browse events, enrol in categories, and view their personal results.
- **Public users** to browse events, categories, and verified results.

Authentication is **session-based** with **BCrypt password hashing** and **role-based access control** enforced on every endpoint.

---

## 🏗️ Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core Web API 8.0 |
| Language | C# 12 |
| Database | SQL Server (LocalDB or Express) |
| ORM | Entity Framework Core 8 (Code-First) |
| Authentication | Session-based with role claims |
| Password Hashing | BCrypt.Net-Next |
| Documentation | Swagger / OpenAPI (Swashbuckle) |
| Testing | xUnit, Moq, FluentAssertions, EF Core InMemory |
| CI/CD | GitHub Actions |

---

## 👥 User Roles

### Organiser
- Register and log in with an Organiser account.
- Create, update, and delete **their own events**.
- Define **categories** (age groups, distances, entry fees) for their events.
- View **all enrolments** for their events.
- Capture **results** (finish times, ranks) for participants.

### Participant
- Register and log in with a Participant account.
- Browse **all active events** and view categories.
- **Enrol** in a chosen event category.
- View their **personal enrolments and results**.

### Public (unauthenticated)
- Browse events, categories, and verified event results.

---

## 🔐 Security & Authorization

- **Passwords are hashed with BCrypt** before being stored — never stored in plain text.
- **Session-based authentication** stores `UserId` and `Role` in the session cookie.
- A custom `[AuthorizeRole("Organiser")]` attribute enforces role-based access.
- **Ownership checks** ensure Organisers can only modify their own events, categories, and results.
- **Participants cannot access Organiser endpoints** and vice versa — enforced by the custom attribute and verified by unit tests.

---

## 🚀 Setup Instructions

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB is fine) or SQL Server Express
- Visual Studio 2022 or VS Code (with the C# extension)

<img width="1502" height="433" alt="image" src="https://github.com/user-attachments/assets/c1fe5e9c-9bae-44ec-880a-760637c2284d" />
