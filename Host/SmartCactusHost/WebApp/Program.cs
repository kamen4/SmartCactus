using Service;
using Service.Contracts;
using LoggerService;
using ILogger = LoggerService.ILogger;
using Repository;
using Microsoft.EntityFrameworkCore;
using Repository.Contracts;


namespace WebApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllersWithViews();
        builder.Services.AddSingleton<ILogger, Logger>();
        builder.Services.AddSingleton<IRepositoryManager, RepositoryManager>();
        builder.Services.AddDbContext<RepositoryContext>(opts => 
            opts.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnection")), ServiceLifetime.Singleton);
        builder.Services.AddSingleton<IServiceManager, ServiceManager>();

        var app = builder.Build();

        builder.Configuration.AddJsonFile("private.json");

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}
