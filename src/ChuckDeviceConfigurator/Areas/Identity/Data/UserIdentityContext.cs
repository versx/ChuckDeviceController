namespace ChuckDeviceConfigurator.Data
{
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    public class UserIdentityContext : IdentityDbContext<ApplicationUser>
    {
        public UserIdentityContext(DbContextOptions<UserIdentityContext> options)
            : base(options)
        {
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
}