using Attendance.Domain.Entities;
using Attendance.Domain.ValueObjects;
using Attendance.Infrastructure.Persistence;
using Bogus;
using Microsoft.EntityFrameworkCore;

var connArg = args.FirstOrDefault(a => a.StartsWith("--conn="))?.Split('=', 2)[1]
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=AttendanceTenantA;Trusted_Connection=true;TrustServerCertificate=true";
var userCount = int.Parse(args.FirstOrDefault(a => a.StartsWith("--users="))?.Split('=', 2)[1] ?? "500");
var days = int.Parse(args.FirstOrDefault(a => a.StartsWith("--days="))?.Split('=', 2)[1] ?? "30");

Console.WriteLine($"Seeding {userCount} users x {days} days of punches into:");
Console.WriteLine($"  {connArg}");

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(connArg)
    .Options;

await using var db = new AppDbContext(options);
await db.Database.EnsureCreatedAsync();

if (await db.Users.AnyAsync())
{
    Console.WriteLine("Users already exist. Clearing punches but keeping users.");
    db.Punches.RemoveRange(db.Punches);
    await db.SaveChangesAsync();
}
else
{
    var fakerUser = new Faker<User>()
        .CustomInstantiator(f =>
        {
            var ext = ExternalRef.Parse($"EXT{f.IndexFaker:00000}");
            var name = f.Name.FullName();
            var role = f.IndexFaker switch
            {
                var i when i < userCount * 9 / 10 => UserRole.Student,
                var i when i < userCount * 95 / 100 => UserRole.Faculty,
                _ => UserRole.Staff
            };
            return new User(ext, name, role, DateOnly.FromDateTime(f.Date.Past(2)));
        });

    var users = fakerUser.Generate(userCount);
    db.Users.AddRange(users);
    await db.SaveChangesAsync();
    Console.WriteLine($"Inserted {users.Count} users.");
}

var allUserIds = await db.Users.Where(u => u.IsActive).Select(u => u.Id).ToListAsync();
var faker = new Faker();
var startDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-days);
var batchId = $"seed-{DateTime.UtcNow:yyyyMMddHHmmss}";

var punches = new List<Punch>(capacity: 1024);
var total = 0;

for (var d = 0; d < days; d++)
{
    var date = startDate.AddDays(d);
    if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;

    var baseDate = date.ToDateTime(TimeOnly.MinValue);

    foreach (var userId in allUserIds)
    {
        if (faker.Random.Double() < 0.05) continue; // ~5% absent

        var inAt = new DateTimeOffset(baseDate.AddMinutes(540 + faker.Random.Int(0, 60)), TimeSpan.Zero);
        var outAt = new DateTimeOffset(baseDate.AddMinutes(1020 + faker.Random.Int(0, 90)), TimeSpan.Zero);
        var device = $"DEV-{(userId % 10) + 1:00}";

        punches.Add(new Punch(userId, inAt, device, Direction.In, batchId));
        if (faker.Random.Double() > 0.10) // ~10% miss OUT
        {
            punches.Add(new Punch(userId, outAt, device, Direction.Out, batchId));
        }

        if (punches.Count >= 1000)
        {
            db.Punches.AddRange(punches);
            await db.SaveChangesAsync();
            total += punches.Count;
            punches.Clear();
        }
    }
}

if (punches.Count > 0)
{
    db.Punches.AddRange(punches);
    await db.SaveChangesAsync();
    total += punches.Count;
}

Console.WriteLine($"Seeded {total} punches across {days} days for {allUserIds.Count} users.");
