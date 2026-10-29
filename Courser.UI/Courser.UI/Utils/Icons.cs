using System;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;

namespace Courser.UI.Utils;

public static class Icons
{
    private const string IconsResourceNamePrefix = "Courser.UI.Icons.";
    private const string LectureIconsResourceNamePrefix = IconsResourceNamePrefix + "Lectures.";
    private static readonly Lazy<Icon>[] LectureIcons;
    private static readonly Dictionary<string, int> CachedLectureIcons = [];

    public static Icon DiamondIcon { get; private set; }
    public static Icon ErrorIcon { get; private set; }

    static Icons()
    {
        var assembly = typeof(Icons).Assembly;
        LectureIcons =
        [
            .. assembly.GetManifestResourceNames()
                .Where(e => e.StartsWith(LectureIconsResourceNamePrefix))
                .Order()
                .Select(e => new Lazy<Icon>(() => UI.Get(() => Icon.FromResource(e, assembly))))
        ];

        DiamondIcon = Icon.FromResource(IconsResourceNamePrefix + "icon-dia.ico", assembly);
        ErrorIcon = Icon.FromResource(IconsResourceNamePrefix + "icon-barrier.ico", assembly);
    }

    public static Icon GetLectureIcon(string lectureName)
    {
        if (!CachedLectureIcons.TryGetValue(lectureName, out var iconIndex))
        {
            var nameHash = lectureName.Aggregate(0, (current, c) => current + c);
            do
            {
                iconIndex = nameHash++ % LectureIcons.Length;
            } while (CachedLectureIcons.Count < LectureIcons.Length && CachedLectureIcons.ContainsValue(iconIndex));

            CachedLectureIcons.Add(lectureName, iconIndex);
        }
        return LectureIcons[iconIndex].Value;
    }

}
