using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CMCS_ST10337861.Data;
using CMCS_ST10337861.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ??
                     "Data Source=cmcs.db"));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// FULLY WORKING SEEDING — STARTS HERE
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();

    // Seed Roles
    string[] roleNames = { "Lecturer", "ProgrammeCoordinator", "AcademicManager" };
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Seed Test Users
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var users = new[]
    {
        ("lecturer@cmcs.co.za", "John Lecturer", "Lecturer", "Lec@123"),
        ("coordinator@cmcs.co.za", "Sarah Coordinator", "ProgrammeCoordinator", "Coo@123"),
        ("manager@cmcs.co.za", "Mike Manager", "AcademicManager", "Man@123")
    };

    foreach (var (email, name, role, pass) in users)
    {
        if (await userManager.FindByEmailAsync(email) == null)
        {
            var user = new ApplicationUser { UserName = email, Email = email, FullName = name, EmailConfirmed = true };
            await userManager.CreateAsync(user, pass);
            await userManager.AddToRoleAsync(user, role);
        }
    }
}
// SEEDING ENDS HERE

app.Run();