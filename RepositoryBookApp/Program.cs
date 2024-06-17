using Microsoft.EntityFrameworkCore;
using RepositoryBookApp.Data;
using RepositoryBookApp.Interfaces;
using RepositoryBookApp.Repositories;

namespace RepositoryBookApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            //Hier :
            builder.Services.AddDbContext<RepoContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("RepoConn"));
            });

            builder.Services.AddControllersWithViews();

            //registreren (Dependency Injection):
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=books}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
