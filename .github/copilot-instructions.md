# SMKCAPI - .NET 4.5 Web API Project Instructions

This is a secure .NET 4.5 Web API project designed for banking integration with SHA-based authentication.

## Project Overview
- **Framework**: .NET Framework 4.5
- **Project Name**: smkcapi
- **Security**: SHA-based authentication with robust authorization
- **Target Server**: Windows Server 2012 R2
- **Purpose**: Banking API integration

## Architecture
The project follows best practices with:
- Controllers for API endpoints
- Models for data structures
- Security filters for authentication/authorization
- Repository pattern for data access
- Dependency injection container
- Comprehensive logging and error handling

## Security Features
- SHA-256 based request signing
- API key authentication
- Request timestamp validation
- IP whitelist validation
- Rate limiting
- Secure headers implementation

## Development Guidelines
- Follow RESTful API conventions
- Implement proper error handling
- Use dependency injection for testability
- Maintain comprehensive logging
- Validate all inputs
- Use HTTPS only in production