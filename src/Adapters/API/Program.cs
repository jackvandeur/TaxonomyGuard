using EnterpriseGovernance.Adapters.M365;
using EnterpriseGovernance.Adapters.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;


var builder = WebApplication.CreateBuilder(args);

// 1. Voeg controllers toe aan de container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 2. Registreer onze eigen M365 Adapter services
builder.Services.AddSingleton<SharePointMappingService>();
builder.Services.AddScoped<SharePointTenantScanner>();

// 3. Registreer de basis PnP Core services (zonder globale provider)
builder.Services.AddPnPCore();

// 3b. Configureer de SQLite Database Adapter (migraties landen nu standaard in de database-assembly)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=governance.db";
builder.Services.AddDbContext<GovernanceDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();


// 5. Automatisch database aanmaken/updaten bij opstarten
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<GovernanceDbContext>();
        // Dit voert automatisch alle openstaande migraties uit op de SQLite database
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Er is een fout opgetreden tijdens het initialiseren van de database.");
    }
}

app.Run();