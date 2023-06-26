using API.Data;
using API.Helpers;
using API.Interfaces;
using API.Services;
using API.SignalR;
using Microsoft.EntityFrameworkCore;

namespace API.Extensions
{
  public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration){
          services.AddDbContext<DataContext>(opt => 
          {
            opt.UseSqlite(configuration.GetConnectionString("DefaultConnection"));
          });
          
          services.AddCors();
          services.AddScoped<ITokenService, TokenService>(); // scoped to http request, transient too short-lived; singleton example: caching service. You want an interface so that you can mock
          services.AddScoped<IUserRepository, UserRepository>(); //adding scoped here makes it injectable
          services.AddScoped<IPhotoService, PhotoService>();
          services.AddScoped<ILikesRepository, LikesRepository>();
          services.AddScoped<IMessageRepository, MessageRepository>();
          services.AddScoped<LogUserActivity>();
          services.AddSingleton<PrescenceTracker>(); //need singleton here to make sure it lives for the life of the application
          
          services.AddSignalR();

          services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

          services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));

          return services;
        }
    }
}
