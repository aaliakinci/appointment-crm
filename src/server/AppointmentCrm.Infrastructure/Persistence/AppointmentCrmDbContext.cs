using Microsoft.EntityFrameworkCore;

namespace AppointmentCrm.Infrastructure.Persistence;

public sealed class AppointmentCrmDbContext(
    DbContextOptions<AppointmentCrmDbContext> options) : DbContext(options)
{
}
