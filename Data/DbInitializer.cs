using Microsoft.AspNetCore.Identity;
using PCPartsHub.Models;

namespace PCPartsHub.Data;

public static class DbInitializer
{
    // These accounts are only used when running locally in Development.
    private const string DevelopmentAdminEmail = "admin@pcshop.com";
    private const string DevelopmentAdminPassword = "Admin12345";
    private const string DevelopmentAdminFullName = "PCShop Administrator";

    private const string DevelopmentSellerEmail = "seller@pcshop.com";
    private const string DevelopmentSellerPassword = "Seller12345";
    private const string DevelopmentSellerFullName = "PCShop Official Seller";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();

        RoleManager<IdentityRole> roleManager =
            scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        IConfiguration configuration =
            scope.ServiceProvider
                .GetRequiredService<IConfiguration>();

        IHostEnvironment environment =
            scope.ServiceProvider
                .GetRequiredService<IHostEnvironment>();

        // Always make sure the required roles exist.
        await CreateRolesAsync(roleManager);

        if (environment.IsDevelopment())
        {
            // Local testing accounts.
            await CreateOrUpdateUserAsync(
                userManager,
                DevelopmentAdminEmail,
                DevelopmentAdminPassword,
                DevelopmentAdminFullName,
                "Admin");

            await CreateOrUpdateUserAsync(
                userManager,
                DevelopmentSellerEmail,
                DevelopmentSellerPassword,
                DevelopmentSellerFullName,
                "Seller");
        }
        else
        {
            // Production administrator.
            // The email and password come from Azure Environment Variables.
            await CreateProductionAdminAsync(
                userManager,
                configuration);
        }
    }

    private static async Task CreateRolesAsync(
        RoleManager<IdentityRole> roleManager)
    {
        string[] roles =
        {
            "Admin",
            "Seller",
            "Buyer"
        };

        foreach (string roleName in roles)
        {
            bool roleExists =
                await roleManager.RoleExistsAsync(roleName);

            if (roleExists)
            {
                continue;
            }

            IdentityResult result =
                await roleManager.CreateAsync(
                    new IdentityRole(roleName));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create role '{roleName}': " +
                    FormatErrors(result));
            }
        }
    }

    private static async Task CreateProductionAdminAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        string? email =
            configuration["SeedAdmin:Email"];

        string? password =
            configuration["SeedAdmin:Password"];

        string fullName =
            configuration["SeedAdmin:FullName"]
            ?? "PCShop Administrator";

        // Do not create an account if Azure settings are missing.
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        await CreateOrUpdateUserAsync(
            userManager,
            email,
            password,
            fullName,
            "Admin");
    }

    private static async Task CreateOrUpdateUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string fullName,
        string roleName)
    {
        ApplicationUser? user =
            await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                RegisteredDate = DateTime.Now
            };

            IdentityResult createResult =
                await userManager.CreateAsync(
                    user,
                    password);

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create '{email}': " +
                    FormatErrors(createResult));
            }
        }

        bool alreadyInRole =
            await userManager.IsInRoleAsync(
                user,
                roleName);

        if (!alreadyInRole)
        {
            IdentityResult roleResult =
                await userManager.AddToRoleAsync(
                    user,
                    roleName);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to assign the '{roleName}' role " +
                    $"to '{email}': " +
                    FormatErrors(roleResult));
            }
        }
    }

    private static string FormatErrors(
        IdentityResult result)
    {
        return string.Join(
            ", ",
            result.Errors.Select(
                error => error.Description));
    }
}