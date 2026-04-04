Multi-Tenant SaaS Engine - .NET 8 & Angular 17
This project is a robust, scalable Multi-Tenant SaaS engine built with a "Database-per-Tenant" strategy. It focuses on high-level data isolation, automated infrastructure provisioning, and secure identity management, providing a production-ready foundation for any industry requiring strict data segregation.

Technical Core
Multi-Tenancy Logic
The system implements a physical isolation strategy. Each organization (tenant) resides in its own SQL Server database, managed by a Master Database that handles metadata and routing.

Backend Implementation (.NET 8)
On-the-fly Provisioning: The TenantService automatically detects, creates, and migrates client databases upon the first valid login, eliminating manual DBA intervention.

Dynamic Context Middleware: A custom middleware extracts the tenant identity from a secure header, injecting the correct database connection string into the request pipeline.

Automatic Audit System: Using EF Core Interceptors, the platform handles metadata (CreatedAt/UpdatedAt) globally, ensuring data integrity without repetitive code.

Frontend Implementation (Angular 17)
Generic Route Handling: Uses dynamic path segments to support an unlimited number of tenants under a single deployment.

Security: Integrated with Google OAuth2 for enterprise-grade authentication.

Getting Started
Prerequisites
Docker Desktop

Google Cloud Console Credentials

Running the Stack
Clone the repository.

Run the orchestration command:

Bash
docker-compose up --build
Access the environment:

Application: http://localhost:4200/[tenant-name]/login

API Documentation: http://localhost:7094/swagger