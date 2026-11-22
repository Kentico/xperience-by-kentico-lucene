using System;

using CMS.EmailEngine;

using Kentico.Membership;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DancingGoat;

/// <summary>
/// Configures email sending and local development options from the configuration ('appsettings.Development.json',
/// 'appsettings.CI.json'). Kept out of 'Program.cs' so Dancing Goat template updates do not conflict with it.
/// </summary>
public static class LocalEnvironmentConfiguration
{
    private const string SMTP_SECTION = "SmtpOptions";
    private const string SYSTEM_EMAIL_SECTION = "SystemEmailOptions";
    private const string EMAIL_QUEUE_SECTION = "EmailQueueOptions";
    private const string ADMIN_EXPIRE_MINUTES_CONFIGURATION_KEY = "AdminIdentityOptions:ExpireTimeSpanMinutes";


    /// <summary>
    /// Registers SMTP (the fallback for system emails, notifications, and every email channel without its own
    /// configuration), binds <see cref="SmtpOptions"/>, <see cref="SystemEmailOptions"/>, and <see cref="EmailQueueOptions"/>
    /// to their sections and, when <c>AdminIdentityOptions:ExpireTimeSpanMinutes</c> is set, overrides the administration
    /// authentication cookie expiration.
    /// </summary>
    /// <remarks>
    /// <c>CMSProcessContactActionsInterval</c> needs no registration; Xperience reads it from the configuration.
    /// </remarks>
    /// <param name="builder">The web application builder.</param>
    public static void AddDancingGoatLocalEnvironment(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("CI"))
        {
            return;
        }

        builder.Services.AddXperienceSmtp();
        builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SMTP_SECTION));
        builder.Services.Configure<SystemEmailOptions>(builder.Configuration.GetSection(SYSTEM_EMAIL_SECTION));
        builder.Services.Configure<EmailQueueOptions>(builder.Configuration.GetSection(EMAIL_QUEUE_SECTION));

        if (builder.Configuration.GetValue<int?>(ADMIN_EXPIRE_MINUTES_CONFIGURATION_KEY) is { } minutes)
        {
            if (minutes <= 0)
            {
                throw new InvalidOperationException($"{ADMIN_EXPIRE_MINUTES_CONFIGURATION_KEY} must be positive.");
            }

            // Post-configuration wins over the demo expiration set in 'Program.cs'
            builder.Services.PostConfigure<AdminIdentityOptions>(options =>
                options.AuthenticationOptions.ExpireTimeSpan = TimeSpan.FromMinutes(minutes));
        }
    }
}
