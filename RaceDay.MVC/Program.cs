using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. Configure HttpClient to talk to the API
builder.Services.AddHttpClient("RaceDayAPI", client =>
{
    // Use host.docker.internal so the Docker container can reach your host machine's API.
    // If you are running the MVC app normally (not in Docker), this will still work on Windows.
    client.BaseAddress = new Uri("https://host.docker.internal:7119/");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    // Bypass SSL certificate validation for local development (because the API uses a self-signed cert)
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };
});

// 2. Add Session for the MVC app (to store the API session cookie)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 3. Register ApiService and HttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<RaceDay.MVC.Services.ApiService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// We can disable HTTPS redirection inside the container to avoid port conflicts during testing
// app.UseHttpsRedirection(); 
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();