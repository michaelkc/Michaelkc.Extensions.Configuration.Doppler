# Michaelkc.Extensions.Configuration.Doppler

`DopplerSDK.ConfigurationProvider` adds a read-only Doppler-backed configuration provider to the standard .NET configuration pipeline.

It is designed to be used like other built-in providers: register once during startup, then bind your options as usual.

## Features

- `AddDoppler(...)` integration for `IConfigurationBuilder` / `ConfigurationManager`
- Read-only secret loading from Doppler's download endpoint
- Token-first bootstrap (`DopplerToken` required)
- Optional host override and Doppler name transformer selection
- Optional periodic reload support
- Fail-fast startup behavior by default when Doppler loading fails

## Quick start

```csharp
using DopplerSDK.ConfigurationProvider;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDoppler(doppler =>
{
    // DopplerToken could come from environment or user secrets
    doppler.DopplerToken = builder.Configuration["DopplerToken"];
    doppler.DopplerNameTransformer = DopplerNameTransformers.DotNet;
    // doppler.DopplerApiHost = "https://api.doppler.com"; // optional override
});
```

Then configure options as normal:

```csharp
builder.Services.Configure<AppSettings>(builder.Configuration);
```

## Bootstrap configuration contract

`DopplerClientConfiguration` fields:

- `DopplerToken` (**required**)
- `DopplerNameTransformer` (optional, defaults to `dotnet`)
- `DopplerApiHost` (optional, defaults to `https://api.doppler.com`)
- `RequestTimeout` (optional, defaults to 30 seconds)

Provider-level fields:

- `FailFast` (optional, defaults to `true`)
- `ReloadInterval` (optional, disabled unless set)

## Security guidance

- Do not hard-code service tokens in committed files.
- Prefer environment variables, user secrets, or a local development-only JSON file ignored by git.
- Treat Doppler tokens as credentials with least-privilege access.

## Sample

See the [SampleApp](./samples/SampleApp) project for an end-to-end example. It also demonstrates how to load an entire appsettings.json document out of a single key using the provider.
