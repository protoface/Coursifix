using System;
using System.IO;
using System.Runtime;
using System.Text.Json;
using Courser.Common;
using Courser.Common.Models;
using Courser.UI.Utils;
using Eto.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Courser.UI;

public static class EntryPoint
{
    private const string preferenceFile = "preferences.json";

    public static void Enter(Application application)
    {
        application.Theme = Themes.System;
        ProfileOptimization.SetProfileRoot(Environment.CurrentDirectory);
        ProfileOptimization.StartProfile("profile.jit");
        EnsurePreferenceFile();

        var config = new ConfigurationBuilder().AddJsonFile(preferenceFile, false, true).Build();
        Services.ServiceProvider = new ServiceCollection()
            .AddCourserServices(config)
            .AddFormServices()
            .BuildServiceProvider();

        application.Run(Services.GetService<MainForm>());
    }

    private static IServiceCollection AddFormServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<MainForm>();
        serviceCollection.AddTransient<CalendarForm>();
        return serviceCollection;
    }


    private static void EnsurePreferenceFile()
    {
        if (!File.Exists(preferenceFile))
        {
            File.WriteAllText(preferenceFile, JsonSerializer.Serialize(new CourseConfig() { Course = "MA-TINF26IT" }, new JsonSerializerOptions() { WriteIndented = true }));
        }
    }
}
