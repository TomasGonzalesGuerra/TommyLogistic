using TommyLogistic.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace TommyLogistic.API.Data;

public class LogisticDataContext(DbContextOptions<LogisticDataContext> options) : IdentityDbContext<User>(options)
{
}
