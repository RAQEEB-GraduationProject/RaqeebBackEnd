using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RAQEEB.Entities;

namespace RAQEEB.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // 1. Seed roles
            string[] roles =
            {
                "Admin",
                "Doctor",
                "Nurse",
                "Caregiver",
                "Patient"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }

            // 2. Seed Hospital
            var hospital = await context.Hospitals
                .FirstOrDefaultAsync();

            if (hospital == null)
            {
                hospital = new Hospital
                {
                    Id = Guid.NewGuid(),
                    Name = "RAQEEB Demo Hospital",
                    Address = "Giza, Egypt",
                    IsActive = true
                };

                context.Hospitals.Add(hospital);

                await context.SaveChangesAsync();
            }

            // 3. Seed Admin
            var adminEmail = "admin@raqeeb.com";

            var admin = await userManager
                .FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    PhoneNumber = "01000000000",
                    HospitalId = hospital.Id,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(
                    admin,
                    "Admin@12345"
                );

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description));

                    throw new Exception(
                        $"Failed to create admin: {errors}");
                }
            }

            // 4. Assign Admin role
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}