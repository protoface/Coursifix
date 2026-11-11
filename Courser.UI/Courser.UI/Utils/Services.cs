using System;
using Microsoft.Extensions.DependencyInjection;

namespace Courser.UI.Utils;

internal static class Services
{
    public static IServiceProvider ServiceProvider { get; set; } = null!;

    public static T Get<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();
}
