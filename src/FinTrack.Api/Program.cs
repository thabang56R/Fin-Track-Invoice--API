using System.Text;
using System.Text.Json.Serialization;
using FinTrack.Api.Security;
using FinTrack.Infrastructure.Auditing;
using FinTrack.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ✅ Controllers + JSON cycle handling
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// ✅ Swagger + JWT Authorize button
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FinTrack API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer", 
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


builder.Services.AddScoped<IUserContext, HttpUserContext>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();


builder.Services.AddDbContext<AppDbContext>((sp, opt) =>
{
    var interceptor = sp.GetRequiredService<AuditSaveChangesInterceptor>();

    var conn =
        builder.Configuration.GetConnectionString("Sql")
        ?? builder.Configuration["ConnectionStrings__Sql"]
        ?? throw new InvalidOperationException("Missing connection string 'Sql' (ConnectionStrings:Sql or ConnectionStrings__Sql).");

    opt.UseSqlServer(conn);
    opt.AddInterceptors(interceptor);
});


var jwt = builder.Configuration.GetSection("Jwt");
var issuer = jwt["Issuer"] ?? builder.Configuration["Jwt__Issuer"] ?? "FinTrack";
var audience = jwt["Audience"] ?? builder.Configuration["Jwt__Audience"] ?? "FinTrack";
var keyString = jwt["Key"] ?? builder.Configuration["Jwt__Key"]
    ?? throw new InvalidOperationException("Jwt Key is missing. Set Jwt:Key or Jwt__Key.");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();


app.UseStaticFiles();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinTrack API v1");
        c.DisplayRequestDuration();
    });
}
else
{
    
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();








