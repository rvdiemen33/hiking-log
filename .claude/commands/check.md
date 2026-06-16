Run the full HikingLog verification sequence in order:

```powershell
dotnet build
dotnet format --verify-no-changes
dotnet test tests/HikingLog.Application.Tests
dotnet test tests/HikingLog.Api.Tests
```

Report which steps pass and which fail. Stop at the first failure and explain what went wrong.
