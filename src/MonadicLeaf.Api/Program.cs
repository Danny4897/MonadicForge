using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MonadicLeaf.Api.Middleware;
using MonadicLeaf.Modules.Tenants;
using MonadicLeaf.Modules.Tenants.Application.Commands;
using MonadicLeaf.Modules.Tenants.Application.Queries;
using MonadicLeaf.Modules.Tenants.Contracts;
using MonadicLeaf.Modules.Tenants.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ─── Database ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<TenantDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// ─── Tenants module ──────────────────────────────────────────────────────────
builder.Services.AddScoped<TenantRepository>();
builder.Services.AddScoped<GetTenantQuery>();
builder.Services.AddScoped<CreateTenantCommand>();
builder.Services.AddScoped<UpdateTenantPlanCommand>();
builder.Services.AddScoped<IncrementUsageCommand>();
builder.Services.AddScoped<ITenantsService, TenantsService>();

// ─── Auth ────────────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required in configuration");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ─── API ─────────────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MonadicLeaf API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new()
    {
        [new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});

// ─── App ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();

// Middleware order matters: TenantMiddleware → PlanEnforcementMiddleware → Controllers
app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<PlanEnforcementMiddleware>();

app.UseAuthorization();
app.MapControllers();

app.Run();
