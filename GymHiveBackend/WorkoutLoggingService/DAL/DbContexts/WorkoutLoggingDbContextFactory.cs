using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace WorkoutLoggingService.DAL.DbContexts;

public class WorkoutLoggingDbContextFactory : IDesignTimeDbContextFactory<WorkoutLoggingDbContext>
{
    public WorkoutLoggingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WorkoutLoggingDbContext>();
        
        // Design-time connection string
        optionsBuilder.UseMySql(
            "Server=localhost;Port=3306;Database=GymHiveWorkoutLogs;User=root;Password=root;",
            new MySqlServerVersion(new Version(8, 0, 26)));

        return new WorkoutLoggingDbContext(optionsBuilder.Options);
    }
}
