using DopplerSDK.ConfigurationProvider;
using SampleApp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Use a local development-only file for bootstrap token settings when needed.
builder.Configuration.AddJsonFile("dopplerClientConfig.Development.json", optional: true);

builder.Configuration.AddDoppler(doppler =>
{
    doppler.DopplerToken = builder.Configuration["DopplerToken"];
    doppler.DopplerNameTransformer = builder.Configuration["DopplerNameTransformer"] ??
                                     DopplerNameTransformers.DotNet;

    var configuredApiHost = builder.Configuration["DopplerApiHost"];
    if (!string.IsNullOrWhiteSpace(configuredApiHost)) doppler.DopplerApiHost = configuredApiHost;
});

builder.Services.Configure<AppSettings>(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
