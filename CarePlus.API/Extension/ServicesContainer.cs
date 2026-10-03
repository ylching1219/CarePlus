using CarePlus.API.Data;
using CarePlus.API.Repository;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.UserServices;
using CarePlus.API.Services.UserServices.Interface;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Extension
{
    public static class ServicesContainer
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration config)
        {
            //db connection
            var connectString = config.GetConnectionString("DefaultConnection");
            services.AddDbContext<AppDbContext>(options =>
                options.UseMySQL(connectString!));

            //services
            services.AddScoped<IUserServices, UserServices>();

            //repository
            services.AddScoped<IUserRepository, UserRepository>();
            return services;
        }
    }
}
