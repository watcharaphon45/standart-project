using clean.Application.Common.Interfaces;
using clean.Infrastructure.Identity;
using clean.Infrastructure.Persistence;
using clean.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace clean.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Set ConnectionStrings__Database before starting the API.");
        services.AddSingleton(TimeProvider.System);
        services.AddScoped(_ => new NpgsqlConnection(connectionString));
        services.AddDbContext<OrganizationDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<NpgsqlConnection>(), pg => pg.MigrationsHistoryTable("__DbMigrationsHistory")));
        services.AddDbContext<SystemDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<NpgsqlConnection>(), pg => pg.MigrationsHistoryTable("__SuMigrationsHistory")));
        services.AddScoped<IOrganizationDbContext>(provider => provider.GetRequiredService<OrganizationDbContext>());
        services.AddScoped<IOrganizationLookup, OrganizationLookup>();
        services.AddScoped<IAtomicWriter, AtomicWriter>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<AccessService>();
        services.AddScoped<IAccessService>(provider => provider.GetRequiredService<AccessService>());
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ISystemAdminService, SystemAdminService>();
        services.AddScoped<BootstrapService>();
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._";
            options.Password.RequiredLength = 15;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddRoles<ApplicationRole>().AddEntityFrameworkStores<SystemDbContext>()
          .AddPasswordValidator<ScreenedPasswordValidator>();
        services.AddHttpClient<IPasswordScreening, PwnedPasswordScreening>(client =>
        {
            client.BaseAddress = new Uri("https://api.pwnedpasswords.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).RemoveAllLoggers();
        return services;
    }
}
