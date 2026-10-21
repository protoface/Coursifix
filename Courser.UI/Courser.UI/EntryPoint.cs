using System.IO;
using System.Text.Json;
using Courser.Common;
using Courser.Common.Models;
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

        EnsurePreferenceFile();

        var config = new ConfigurationBuilder().AddJsonFile(preferenceFile, false, true).Build();
        var services = new ServiceCollection().AddCourserServices(config).AddSingleton<MainForm>().BuildServiceProvider();

        application.Run(services.GetRequiredService<MainForm>());
    }



    private static void EnsurePreferenceFile()
    {
        if (!File.Exists(preferenceFile))
        {
            File.WriteAllText(preferenceFile, JsonSerializer.Serialize(new CourseConfig() { Course = "MA-TINF26IT" }, new JsonSerializerOptions() { WriteIndented = true }));
        }
    }
}
