using System;
using Microsoft.Extensions.DependencyInjection;

namespace Courser.UI.Utils;

internal static class Services
{
    public static IServiceProvider ServiceProvider { get; set; }

    public static T GetService<T>() => ServiceProvider.GetRequiredService<T>();
}
