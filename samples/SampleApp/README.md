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

Recommended options:

- Environment variable
- Local development-only JSON file (ignored by git), for example: `dopplerClientConfig.Development.json`

Create a local token file with Doppler CLI:

```sh
echo "{ \"DopplerToken\": \""$(doppler configs tokens create dev --plain)"\" }" > dopplerClientConfig.Development.json
```

## How the sample uses the provider

`Program.cs` registers Doppler as a configuration provider:

```csharp
builder.Configuration.AddDoppler(doppler =>
{
    doppler.DopplerToken = builder.Configuration["DopplerToken"];
    doppler.DopplerNameTransformer = DopplerNameTransformers.DotNet;
});
```

After registration, standard options binding works the same way as any other configuration source:

```csharp
builder.Services.Configure<AppSettings>(builder.Configuration);
```

## Run

Run/debug the sample app and open the index page to inspect bound values from Doppler.
