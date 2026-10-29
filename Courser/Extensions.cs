using System;

namespace Courser;

public static class Extensions
{
    extension(DateTimeOffset time)
    {
        public string ToLocalShortTimeString() => time.LocalDateTime.ToShortTimeString();
    }
}
