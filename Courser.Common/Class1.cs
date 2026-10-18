using Courser.Common.Interfaces;
using Courser.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Courser.Common.Models;

namespace Courser.Common;

public static class Extensions
{

    extension(IServiceCollection serviceCollection)
    {
        public IServiceCollection AddCourserServices(IConfiguration config)
        {
            serviceCollection.AddSingleton<ICalendarService, CalCalendarService>();
            serviceCollection.AddOptions<CourseConfig>().Bind(config);
            return serviceCollection;
        }
    }

}
