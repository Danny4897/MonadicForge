using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MonadicLeaf.Analyzer.Core;
using MonadicLeaf.Api.Middleware;
using MonadicLeaf.Modules.Analyze;
using MonadicLeaf.Modules.Analyze.Application.Commands;
using MonadicLeaf.Modules.Analyze.Application.Llm;
using MonadicLeaf.Modules.Analyze.Contracts;
using MonadicLeaf.Modules.Analyze.Infrastructure.Persistence;
using MonadicLeaf.Modules.Auth;
using MonadicLeaf.Modules.Auth.Application.Commands;
using MonadicLeaf.Modules.Auth.Application.Services;
using MonadicLeaf.Modules.Auth.Contracts;
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

// ─── Analyze module ──────────────────────────────────────────────────────────
builder.Services.AddSingleton<AnalysisEngine>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<AnthropicClient>(sp =>
{
    var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
    var config = sp.GetRequiredService<IConfiguration>();
    return new AnthropicClient(httpFactory.CreateClient("anthropic"), config["Anthropic:ApiKey"]);
});
builder.Services.AddScoped<AnalysisRepository>();
builder.Services.AddScoped<AnalyzeCodeCommand>();
builder.Services.AddScoped<IAnalyzeService, AnalyzeService>();

// ─── Auth module ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<JwtIssuer>();
builder.Services.AddScoped<RegisterCommand>();
builder.Services.AddScoped<LoginCommand>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ─── JWT Auth ────────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required");

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

// ─── CORS ────────────────────────────────────────────────────────────────────
var frontendUrl = builder.Configuration["Frontend:Url"] ?? "http://localhost:5173";
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(policy =>
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod()));

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

// Auto-create schema — use proper EF migrations in production
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<PlanEnforcementMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
