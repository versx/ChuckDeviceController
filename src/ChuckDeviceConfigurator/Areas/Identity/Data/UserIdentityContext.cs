namespace ChuckDeviceConfigurator.Data
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    using ChuckDeviceConfigurator.Areas.Identity.Data;

    public class UserIdentityContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<ApplicationUser> User { get; set; }

        public DbSet<IdentityRole> Role { get; set; }

        public DbSet<IdentityUserRole<string>> UserRoles { get; set; }

        public DbSet<IdentityUserClaim<string>> UserClaims { get; set; }

        public DbSet<IdentityUserLogin<string>> UserLogins { get; set; }

        public DbSet<IdentityRoleClaim<string>> RoleClaims { get; set; }

        public DbSet<IdentityUserToken<string>> UserTokens { get; set; }

        public UserIdentityContext(DbContextOptions<UserIdentityContext> options)
            : base(options)
        {
            // Migrate to latest
            //var createSql = Database.GenerateCreateScript();
            //Console.WriteLine($"CreateSql: {createSql}");
            //base.Database.Migrate();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            /*
            builder.Entity<IdentityUser>().ToTable("identity_users")
                                          .Property(p => p.Id)
                                          .HasColumnName("UserId");
            */
            /*
            builder.Entity<ApplicationUser>().ToTable("identity_users")
                                             .Property(p => p.Id)
                                             .HasColumnName("UserId");
            builder.Entity<IdentityUserRole<string>>().ToTable("identity_user_roles");
            builder.Entity<IdentityUserLogin<string>>().ToTable("identity_user_logins");
            builder.Entity<IdentityUserClaim<string>>().ToTable("identity_user_claims");
            builder.Entity<IdentityRole>().ToTable("identity_roles");
            */
        }
    }

    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }

    public class ManageUserRolesViewModel
    {
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public bool Selected { get; set; }
    }

    public class UserRolesViewModel
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }
}