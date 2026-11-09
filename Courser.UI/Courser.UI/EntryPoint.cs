using System;
using System.IO;
using System.Runtime;
using System.Text.Json;
using Courser.Common;
using Courser.Common.Models;
using Courser.UI.Utils;
using Eto;
using Eto.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Courser.UI;

public static class EntryPoint
{
    private const string preferenceFile = "preferences.json";

    public static void Enter(Application application)
    {
        #if DEBUG
        HotReloadService.Initialize();
        #endif
        
        application.Theme = Themes.System;
        ProfileOptimization.SetProfileRoot(Environment.CurrentDirectory);
        ProfileOptimization.StartProfile("profile.jit");
        EnsurePreferenceFile();

        var config = new ConfigurationBuilder().AddJsonFile(preferenceFile, false, true).Build();
        Services.ServiceProvider = new ServiceCollection()
            .AddCourserServices(config.GetSection("Course"))
            .AddMoodleConfig(config.GetSection("Moodle"))
            .AddFormServices()
            .BuildServiceProvider();

        application.Run(Services.Get<MainForm>());
    }

    extension(IServiceCollection serviceCollection)
    {
        private IServiceCollection AddFormServices()
        {
            serviceCollection.AddSingleton<MainForm>();
            serviceCollection.AddTransient<CalendarForm>();
            serviceCollection.AddTransient<MoodleView>();
            return serviceCollection;
        }

        private IServiceCollection AddMoodleConfig(IConfiguration config)
        {
            serviceCollection.AddOptions<MoodleConfig>().Bind(config);

            return serviceCollection;
        }
    }


    private static void EnsurePreferenceFile()
    {
        if (!File.Exists(preferenceFile))
        {
            File.WriteAllText(preferenceFile, JsonSerializer.Serialize(new { Course = new CourseConfig() { Course = "MA-TINF26IT" }, Moodle = new MoodleConfig() }, new JsonSerializerOptions() { WriteIndented = true }));
        }
    }
}
