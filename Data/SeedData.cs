using LanzaTuIdea.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text; // Necesario para Encoding

namespace LanzaTuIdea.Api.Data;

public static class SeedData
{
    private static readonly string[] DefaultRoles =
    [
        AppConstants.Roles.Admin,
        AppConstants.Roles.Ideador,
        AppConstants.Roles.Gestor
    ];
    private const string DefaultLogoPath = "/assets/branding/logo-placeholder.svg";
    private const string DefaultFaviconPath = "/assets/branding/favicon-placeholder.svg";

    private static readonly (string Proceso, string Subproceso, string Icono, string Descripcion)[] DefaultClassifications =
    [
        ("Planear", "Planificación y Monitoreo", "📊", "Metas, indicadores, seguimiento, reportes y control de planes."),
        ("Planear", "Enfoque al Cliente", "👥", "Experiencia del usuario, atención, satisfacción, quejas y mejora del servicio."),
        ("Planear", "Diseño y Desarrollo", "💡", "Diseño/mejora de programas, contenidos, metodologías y soluciones institucionales."),
        ("Prestación del Servicio", "Formación Profesional", "🎓", "Ejecución de la formación: planificación de ofertas, docencia, evaluación y certificación."),
        ("Prestación del Servicio", "Asesoría y Asistencia Técnica", "🤝", "Acompañamiento a empresas: diagnósticos, asistencia técnica, soluciones y seguimiento."),
        ("Control", "Auditorías Internas (SGC)", "📋", "Auditorías, hallazgos, acciones correctivas/preventivas y mejora del sistema de calidad."),
        ("Control", "Revisión por la Dirección", "👔", "Revisión gerencial del desempeño: resultados, decisiones, prioridades y recursos."),
        ("Soporte", "Recursos Humanos", "❤️", "Gestión de personal: reclutamiento, desarrollo, bienestar, desempeño y nómina."),
        ("Soporte", "Servicios Generales", "🏢", "Servicios de apoyo: mantenimiento, transporte, limpieza, seguridad y facilidades."),
        ("Soporte", "Abastecimiento", "📦", "Compras, almacén, inventarios, proveedores y logística de suministros."),
        ("Soporte", "Finanzas", "💰", "Presupuesto, pagos, contabilidad, ingresos, costos y control financiero."),
        ("Soporte", "Tecnología y Sistemas", "💻", "Soporte TI, sistemas, datos, infraestructura, automatización y seguridad informática."),
        ("Soporte", "Regulación y Supervisión", "⚖️", "Normativas, supervisión, cumplimiento y apoyo regulatorio a centros y operaciones.")
    ];

    public static async Task InitializeAsync(AppDbContext context, IConfiguration configuration, IHostEnvironment environment)
    {
        Console.WriteLine("--> [SeedData] Iniciando inicialización de datos...");
        
        var autoMigrate = environment.IsDevelopment() || configuration.GetValue<bool>("Database:AutoMigrate");
        if (autoMigrate && !context.Database.IsInMemory())
        {
            Console.WriteLine("--> [SeedData] Aplicando migraciones pendientes...");
            await context.Database.MigrateAsync();
        }
        else if (autoMigrate)
        {
            Console.WriteLine("--> [SeedData] Migraciones omitidas (proveedor InMemory).");
        }

        await SeedRolesAsync(context);
        await SeedConfigurationAsync(context);

        if (environment.IsDevelopment())
        {
            await SeedBootstrapAdminsAsync(context, configuration);
        }

        await ImportEmployeesIfEmptyAsync(context, configuration);
        
        Console.WriteLine("--> [SeedData] Proceso finalizado.");
    }

    private static async Task SeedRolesAsync(AppDbContext context)
    {
        var existingRoles = await context.Roles.Select(r => r.Name).ToListAsync();
        var missing = DefaultRoles.Except(existingRoles, StringComparer.OrdinalIgnoreCase).ToList();
        if (missing.Count == 0) return;

        foreach (var role in missing)
        {
            context.Roles.Add(new Role { Name = role });
        }
        await context.SaveChangesAsync();
        Console.WriteLine($"--> [SeedData] Roles creados: {string.Join(", ", missing)}");
    }

    private static async Task SeedConfigurationAsync(AppDbContext context)
    {
        await SeedDefaultClassificationsAsync(context);

        if (!await context.Instances.AnyAsync())
        {
            context.Instances.AddRange(
                new Instance { Nombre = "Sede Central", Activo = true },
                new Instance { Nombre = "Regional Norte", Activo = true }
            );
        }

        if (!await context.BrandingSettings.AnyAsync())
        {
            context.BrandingSettings.Add(new AppBranding
            {
                LogoPath = DefaultLogoPath,
                FaviconPath = DefaultFaviconPath,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDefaultClassificationsAsync(AppDbContext context)
    {
        var existing = await context.Classifications.ToListAsync();

        foreach (var template in DefaultClassifications)
        {
            var current = existing.FirstOrDefault(c =>
                string.Equals(c.Subproceso, template.Subproceso, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.Nombre, template.Subproceso, StringComparison.OrdinalIgnoreCase));

            if (current is null)
            {
                context.Classifications.Add(new Classification
                {
                    Nombre = template.Subproceso,
                    Proceso = template.Proceso,
                    Subproceso = template.Subproceso,
                    Icono = template.Icono,
                    Descripcion = template.Descripcion,
                    Activo = true
                });
                continue;
            }

            current.Nombre = template.Subproceso;
            current.Proceso = template.Proceso;
            current.Subproceso = template.Subproceso;
            current.Icono = template.Icono;
            current.Descripcion = template.Descripcion;
            current.Activo = true;
        }

        var validSubprocesos = DefaultClassifications
            .Select(c => c.Subproceso)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var extra in existing.Where(c => !validSubprocesos.Contains(c.Subproceso ?? c.Nombre)))
        {
            extra.Activo = false;
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedBootstrapAdminsAsync(AppDbContext context, IConfiguration configuration)
    {
        var bootstrapAdmins = configuration.GetSection("BootstrapAdmins").Get<string[]>() ?? Array.Empty<string>();
        if (bootstrapAdmins.Length == 0) return;

        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == AppConstants.Roles.Admin);
        if (adminRole is null) return;

        foreach (var userName in bootstrapAdmins)
        {
            var normalized = userName.Trim();
            if (string.IsNullOrWhiteSpace(normalized)) continue;

            var user = await context.AppUsers.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.UserName == normalized);
            if (user is null)
            {
                user = new AppUser { UserName = normalized, IsActive = true };
                context.AppUsers.Add(user);
                await context.SaveChangesAsync();
            }

            if (!user.UserRoles.Any(ur => ur.RoleId == adminRole.Id))
            {
                user.UserRoles.Add(new UserRole { RoleId = adminRole.Id, UserId = user.Id });
                Console.WriteLine($"--> [SeedData] Admin bootstrap asignado a: {normalized}");
            }
        }
        await context.SaveChangesAsync();
    }

    private static async Task ImportEmployeesIfEmptyAsync(AppDbContext context, IConfiguration configuration)
    {
        var forceReload = configuration.GetValue<bool>("Seed:Employees:ForceReload");
        var hasEmployees = await context.Employees.AnyAsync();
        if (hasEmployees && !forceReload)
        {
            Console.WriteLine("--> [SeedData] Ya existen empleados, se omite la importación.");
            return;
        }

        var root = configuration["Seed:EmployeesPath"] ?? Path.Combine(AppContext.BaseDirectory, "seed", "empleados.csv");
        var path = File.Exists(root) ? root : Path.Combine(Directory.GetCurrentDirectory(), "seed", "empleados.csv");

        Console.WriteLine($"--> [SeedData] Buscando archivo en: {path}");

        if (!File.Exists(path))
        {
            Console.WriteLine("--> [SeedData] ADVERTENCIA: No se encontró el archivo CSV.");
            return;
        }

        if (forceReload)
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            await context.Database.ExecuteSqlRawAsync("DELETE FROM Employees");
            await ImportEmployeesAsync(context, path);
            await transaction.CommitAsync();
            return;
        }

        await ImportEmployeesAsync(context, path);
    }

    private static async Task ImportEmployeesAsync(AppDbContext context, string path)
    {

        // CORRECCIÓN DE ENCODING: Usamos Latin1 para soportar archivos de Excel/Windows en español
        var lines = await File.ReadAllLinesAsync(path, Encoding.Latin1);
        
        if (lines.Length <= 1)
        {
            Console.WriteLine("--> [SeedData] El archivo CSV está vacío o solo tiene cabecera.");
            return;
        }

        var header = lines[0];
        char delimiter = header.Contains(';') ? ';' : ',';
        Console.WriteLine($"--> [SeedData] Delimitador detectado: ' {delimiter} '");

        var employees = new List<Employee>();
        int skipped = 0;

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = ParseCsvLine(line, delimiter);

            // Fallback simple
            if (parts.Count <= 1 && line.Contains(delimiter))
            {
                parts = line.Split(delimiter).Select(p => p.Trim()).ToList();
            }

            if (parts.Count < 5) 
            {
                skipped++;
                Console.WriteLine($"--> [ERROR FORMATO] Línea omitida. Se encontraron {parts.Count} columnas.");
                continue;
            }

            try 
            {
                // Función auxiliar interna para limpiar comillas y espacios
                string Clean(string? value) => value?.Trim().Trim('"').Trim() ?? "";

                var employee = new Employee
                {
                    Codigo_Empleado = Truncate(Clean(parts[0]), 20),
                    Nombre          = parts.Count > 1 ? Truncate(Clean(parts[1]), 100) : "",
                    Apellido1       = parts.Count > 2 ? Truncate(Clean(parts[2]), 100) : "",
                    Apellido2       = parts.Count > 3 ? Truncate(Clean(parts[3]), 100) : "",
                    E_Mail          = parts.Count > 4 ? Truncate(Clean(parts[4]), 200) : "",
                    Departamento    = parts.Count > 5 ? Truncate(Clean(parts[5]), 200) : "", 
                    Estatus         = (parts.Count > 6 && !string.IsNullOrWhiteSpace(parts[6])) 
                                      ? Truncate(Clean(parts[6]), 5) 
                                      : "A"
                };

                if (!string.IsNullOrWhiteSpace(employee.Codigo_Empleado))
                {
                    employees.Add(employee);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> [ERROR MAPPING] Error al procesar línea: {line}. Error: {ex.Message}");
                skipped++;
            }
        }

        if (employees.Count > 0)
        {
            context.Employees.AddRange(employees);
            await context.SaveChangesAsync();
            Console.WriteLine($"--> [SeedData] ÉXITO: Se cargaron {employees.Count} empleados.");
        }
        
        if (skipped > 0) Console.WriteLine($"--> [SeedData] Se omitieron {skipped} líneas por errores.");
    }

    private static List<string> ParseCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        var current = "";
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }
            if (ch == delimiter && !inQuotes)
            {
                result.Add(current.Trim());
                current = "";
                continue;
            }
            current += ch;
        }
        result.Add(current.Trim());
        return result;
    }

    // CORRECCIÓN DEL BUG LÓGICO AQUÍ
    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var val = value.Trim();
        // Si la longitud es menor o igual al máximo, devolvemos el valor COMPLETO.
        // Solo si es mayor, cortamos.
        return val.Length <= maxLength ? val : val.Substring(0, maxLength);
    }
}
