# Doppler Provider Sample App

## Prerequisites

- ASP.NET Core SDK
- [Doppler CLI](https://docs.doppler.com/docs/install-cli) installed

## Setup

Import the sample project secrets to Doppler:

```sh
doppler import
```

Select the config:

```sh
doppler setup --project dotnet-core-webapp --config dev
```

Confirm secrets are available:

```sh
doppler secrets
```

## Bootstrap token

The provider requires a `DopplerToken` from an existing bootstrap source.  
For this sample, assume `DopplerToken` is supplied via User Secrets.

## How the sample uses the provider

`Program.cs` registers Doppler via a wrapper extension method:

```csharp
builder.Configuration.AddMichaelkcDoppler(keysToLoad: ["APP1_APPSETTINGS"]);
```

`AddMichaelkcDoppler(...)` wraps the Doppler provider and supports loading configuration from selected Doppler key(s).  
When a selected key has JSON content, it is expanded into standard colon-delimited .NET configuration paths, and the top-level key name is not retained.

After registration, standard options binding works the same way as any other configuration source:

```csharp
builder.Services.Configure<AppSettings>(builder.Configuration);
```

## Run

Run/debug the sample app and open the index page to inspect bound values from Doppler.
