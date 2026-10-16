using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using RaceDay.API.Data;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// 1. DATABASE
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. SESSION
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".RaceDay.Session";
});

// 3. CONTROLLERS
builder.Services.AddControllers();

// 4. SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay Event Management API",
        Version = "v1",
        Description = "RESTful API for RaceDay. Supports Organisers and Participants with session-based auth and role-based access.",
        Contact = new OpenApiContact { Name = "RaceDay Developer", Email = "developer@raceday.com" }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

    c.AddSecurityDefinition("SessionCookie", new OpenApiSecurityScheme
    {
        Name = ".RaceDay.Session",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Description = "Log in via /api/auth/login and Swagger will use the session cookie."
    });
});

// 5. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// 6. PIPELINE (Swagger in ALL environments — Step 4.6)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "RaceDay API Documentation";
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseSession();
app.UseAuthorization();
app.MapControllers();
app.Run();