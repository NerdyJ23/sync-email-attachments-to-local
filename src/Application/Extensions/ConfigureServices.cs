using Microsoft.EntityFrameworkCore;
using PuppeteerSharp;
using SyncEmailAttachments.Application.Configuration.FileSystem;
using SyncEmailAttachments.Application.Services.Emails;
using SyncEmailAttachments.Application.Services.FileSystem;
using SyncEmailAttachments.Infrastructure.Data.EmailContext;

namespace SyncEmailAttachments.Application.Configuration.Email;

public static class ConfigureServices
{
	public static IServiceCollection AddServices(this IServiceCollection services)
	{
		services.AddSingleton<EmailHandler>();
		services.AddSingleton<EmailClientLogger>();
		services.AddSingleton<FileSystemHandler>();

		services.AddSingleton<BrowserFetcher>();

		return services;
	}

	public static IServiceCollection BuildOptions(this IServiceCollection services, IConfiguration config)
	{
		services.Configure<EmailOptions>(config.GetRequiredSection("EmailOptions"));
		services.Configure<FileSystemOptions>(config.GetRequiredSection("SyncOptions"));

		return services;
	}

	public static IServiceCollection AddDatabases(this IServiceCollection services, IConfiguration config)
	{
		services.AddDbContextFactory<EmailContext>(options => options.UseMySql(
			config.GetConnectionString("EmailContext"),
			ServerVersion.AutoDetect(config.GetConnectionString("EmailContext")))
			.EnableSensitiveDataLogging()
			.EnableDetailedErrors()
		);

		return services;
	}
}