using System;
using System.IO;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: CLSCompliant(false)]
[assembly: FunctionsStartup(typeof(RssToEmail.Startup))]

namespace RssToEmail
{
    public class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();
            var appRoot = context.ApplicationRootPath;

            builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(appRoot, "appsettings.json"), optional: true)
                .AddJsonFile(Path.Combine(appRoot, $"appsettings.{context.EnvironmentName}.json"), optional: true)
                .AddUserSecrets<Startup>()
                .AddEnvironmentVariables();
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOptions<RssToEmailConfig>()
                .Configure<IConfiguration>((options, configuration) =>
                    configuration.GetSection(nameof(RssToEmail)).Bind(options));
            builder.Services.AddSingleton<EmailGenerator>();
            builder.Services.AddEmailServices();
        }
    }
}
