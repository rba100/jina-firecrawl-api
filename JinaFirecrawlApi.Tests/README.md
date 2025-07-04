# JinaFirecrawlApi.Tests

This test project contains comprehensive unit and integration tests for the Jina Firecrawl API.

## Test Structure

### Unit Tests

- **Services/**
  - `ScrapeServiceTests.cs` - Tests for the main scraping service business logic
  - `JinaHandlerTests.cs` - Tests for the Jina API integration handler

- **Controllers/**  
  - `ScrapeControllerTests.cs` - Tests for the REST API controller

- **Middleware/**
  - `ApiKeyMiddlewareTests.cs` - Tests for API key validation middleware

- **Models/**
  - `ModelsTests.cs` - Tests for data models and DTOs

### Integration Tests

- **Integration/**
  - `ScrapeApiIntegrationTests.cs` - End-to-end API tests with mocked dependencies

## Running Tests

Run all tests:
```bash
dotnet test
```

Run with detailed output:
```bash
dotnet test --logger "console;verbosity=detailed"
```

Run with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Coverage

The tests cover:

- ✅ Constructor validation and dependency injection
- ✅ Input validation (null/empty URLs, missing auth headers)
- ✅ PDF URL detection and handling
- ✅ Regular URL processing via Jina
- ✅ Error handling (timeouts, network errors, exceptions)
- ✅ Response formatting and status codes
- ✅ Middleware authorization logic
- ✅ Model property behavior
- ✅ End-to-end API integration

## Test Technologies

- **xUnit** - Test framework
- **Moq** - Mocking framework for dependencies
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing support
- **Microsoft.NET.Test.Sdk** - Test SDK

## Notes

- All external dependencies (Jina API, PDF handlers) are mocked for reliable testing
- Integration tests use `WebApplicationFactory` for realistic testing
- Tests follow AAA pattern (Arrange, Act, Assert)
- Both positive and negative test cases are included