using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TmsApi.Entities;
using TmsApi.Identity;

namespace TmsApi.Data;

// M11 Session 1: now inherits IdentityDbContext<TmsUser> instead of plain
// DbContext. This is what makes UserManager<TmsUser> and RoleManager
// resolvable via DI, and is what the upcoming migration will build the
// seven standard AspNet* Identity tables against.
public class TmsDbContext(DbContextOptions<TmsDbContext> options)
    : IdentityDbContext<TmsUser>(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<Certificate> Certificates => Set<Certificate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // MUST call base first - this applies Identity's own entity
        // configurations (AspNetUsers, AspNetRoles, AspNetUserClaims, etc.).
        // Skipping this would leave those tables unconfigured even though
        // we inherit IdentityDbContext.
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration classes from this assembly
        // Each entity has its own configuration file in the Configurations/ folder
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TmsDbContext).Assembly);
    }
}
