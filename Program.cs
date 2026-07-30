using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WebAppApi.Data;
using WebAppApi.Models;
using WebAppApi.Services;

var builder = WebApplication.CreateBuilder(args);

// EF Core + SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddControllers();

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

// CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Skip HTTPS redirect in Development so React (http://localhost:5173) can call HTTP API
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowReactApp");
app.UseStaticFiles(); // serve wwwroot (avatars at /uploads/avatars/...)
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Seed Admin role + default admin user on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var adminEmail = "admin@webappapi.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Admin",
            IsActive = true
        };
        await userManager.CreateAsync(adminUser, "Admin@123");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
    else
    {
        var changed = false;
        if (string.IsNullOrWhiteSpace(adminUser.FullName))
        {
            adminUser.FullName = "Admin";
            changed = true;
        }
        if (!adminUser.IsActive)
        {
            adminUser.IsActive = true;
            changed = true;
        }
        if (changed)
            await userManager.UpdateAsync(adminUser);
    }


    // Seed app pages (admin + 2 test pages)
    var pagesWereEmpty = !await db.AppPages.AnyAsync();
    var seedPages = new[]
    {
        new AppPage { Name = "Home", Path = "/", Description = "Trang chủ", SortOrder = 1, IsActive = true },
        new AppPage { Name = "My Profile", Path = "/profile", Description = "Thông tin cá nhân", SortOrder = 2, IsActive = true },
        new AppPage { Name = "Quản lý User", Path = "/admin/users", Description = "Tạo/sửa user, reset password, active/inactive", SortOrder = 10, IsActive = true },
        new AppPage { Name = "Quản lý Page", Path = "/admin/pages", Description = "CRUD khai báo page", SortOrder = 11, IsActive = true },
        new AppPage { Name = "Quản lý Role", Path = "/admin/roles", Description = "Quản lý role và gán role cho user", SortOrder = 12, IsActive = true },
        new AppPage { Name = "Phân quyền Page", Path = "/admin/permissions", Description = "Gán page theo role", SortOrder = 13, IsActive = true },
        new AppPage { Name = "Loại xe", Path = "/catalog/vehicle-categories", Description = "Danh mục loại xe", SortOrder = 30, IsActive = true },
        new AppPage { Name = "Mẫu xe", Path = "/catalog/vehicle-models", Description = "Danh mục mẫu xe chi tiết", SortOrder = 31, IsActive = true },
        new AppPage { Name = "Test Page A", Path = "/test/page-a", Description = "Page test A", SortOrder = 20, IsActive = true },
        new AppPage { Name = "Test Page B", Path = "/test/page-b", Description = "Page test B", SortOrder = 21, IsActive = true },
    };

    foreach (var seed in seedPages)
    {
        if (!await db.AppPages.AnyAsync(p => p.Path == seed.Path))
            db.AppPages.Add(seed);
    }
    await db.SaveChangesAsync();

    // First-time page seed: IsActive column defaulted to 0 — activate existing users once
    if (pagesWereEmpty)
    {
        var inactiveUsers = await userManager.Users.Where(u => !u.IsActive).ToListAsync();
        foreach (var u in inactiveUsers)
        {
            u.IsActive = true;
            await userManager.UpdateAsync(u);
        }
    }

    // Grant all pages to Admin role
    var adminRole = await roleManager.FindByNameAsync("Admin");
    if (adminRole != null)
    {
        var allPageIds = await db.AppPages.Select(p => p.Id).ToListAsync();
        foreach (var pageId in allPageIds)
        {
            var exists = await db.RolePagePermissions
                .AnyAsync(p => p.RoleId == adminRole.Id && p.PageId == pageId);
            if (!exists)
            {
                db.RolePagePermissions.Add(new RolePagePermission
                {
                    RoleId = adminRole.Id,
                    PageId = pageId
                });
            }
        }
        await db.SaveChangesAsync();
    }

    // Grant Home + Profile + test pages to User role
    var userRole = await roleManager.FindByNameAsync("User");
    if (userRole != null)
    {
        var userPaths = new[] { "/", "/profile", "/test/page-a", "/test/page-b" };
        var userPageIds = await db.AppPages
            .Where(p => userPaths.Contains(p.Path))
            .Select(p => p.Id)
            .ToListAsync();

        foreach (var pageId in userPageIds)
        {
            var exists = await db.RolePagePermissions
                .AnyAsync(p => p.RoleId == userRole.Id && p.PageId == pageId);
            if (!exists)
            {
                db.RolePagePermissions.Add(new RolePagePermission
                {
                    RoleId = userRole.Id,
                    PageId = pageId
                });
            }
        }
        await db.SaveChangesAsync();
    }
}

app.Run();
