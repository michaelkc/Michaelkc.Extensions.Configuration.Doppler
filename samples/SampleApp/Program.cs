using SampleApp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// JSON key "APP2_APPSETTINGS" in Doppler - the DopplerToken points to project/environment
builder.Configuration.AddMichaelkcDoppler(keysToLoad: ["App2AppSettings"]);

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
