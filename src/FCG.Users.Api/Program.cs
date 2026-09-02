using FCG.BuildingBlocks.Auth;
using FCG.BuildingBlocks.Messaging;
using FCG.BuildingBlocks.Middleware;
using FCG.BuildingBlocks.Observability;
using FCG.Users.Api.Application.Services;
using FCG.Users.Api.Domain.Repositories;
using FCG.Users.Api.Domain.Services;
using FCG.Users.Api.Infrastructure.Data;
using FCG.Users.Api.Infrastructure.Repositories;
using FCG.Users.Api.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "FCG UsersAPI", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization. Use: Bearer {token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddFcgJwtAuth(builder.Configuration);

    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
                  ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection ausente.");
    builder.Services.AddDbContext<UsersDbContext>(opt => opt.UseNpgsql(connStr));

    builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IUsuarioService, UsuarioService>();

    builder.Services.AddFcgMessaging(builder.Configuration, "users");
    builder.Services.AddFcgObservability("users-api");

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        db.Database.EnsureCreated();
    }

    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "UsersAPI" }));
    app.UseFcgObservability();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "UsersAPI encerrada inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
