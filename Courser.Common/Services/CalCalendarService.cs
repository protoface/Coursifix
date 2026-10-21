using System.Diagnostics;
using Courser.Common.Interfaces;
using Courser.Common.Models;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Microsoft.Extensions.Options;

namespace Courser.Common.Services;

internal sealed class CalCalendarService(IOptions<CourseConfig> courseOptions) : ICalendarService
{
    private CourseConfig CourseConfig => courseOptions.Value;
    private List<Lecture> _lectures = [];
    private long _lastCalendarUpdate;

    public Task<Maybe<Lecture>> GetNextEndingLectureAsync()
    {
        var now = DateTime.UtcNow;
        return GetCalendarEvent(e => e.End > now);
    }

    private static Lecture ToLecture(CalendarEvent entry) => new()
    {
        Name = entry.Summary?.Trim() ?? "unnamed lecture",
        Start = entry.Start!.Value,
        End = entry.End!.Value,
        Rooms = entry.Location ?? "No room assigned"
    };

    async Task<Maybe<Lecture>> GetCalendarEvent(Func<Lecture, bool> match)
    {
        if (await UpdateCalendarDataAsync() is { } updateError) return updateError;
        return _lectures.FirstOrDefault(match);
    }

    async Task<Error?> UpdateCalendarDataAsync()
    {
        if (Stopwatch.GetElapsedTime(_lastCalendarUpdate) < CourseConfig.UpdateInterval) return null;
        using var client = new HttpClient();

        switch (await GetICalStream())
        {
            case Stream calStream:
                var calData = Calendar.Load(calStream);
                if (calData is null) return new("Parsing data failed");
                _lectures = [.. calData.Events.Select(ToLecture)];
                await calStream.DisposeAsync();
                break;
            case Error error:
                return error;
        }

        _lastCalendarUpdate = Stopwatch.GetTimestamp();
        return null;
    }

    async Task<Result<Stream>> GetICalStream()
    {
        try
        {
            using var client = new HttpClient();
            var httpResponse = await client.GetAsync("https://dhbw.app/ical/" + CourseConfig.Course);
            if (!httpResponse.IsSuccessStatusCode) return new Error("failed to get calendar data");
            return await httpResponse.Content.ReadAsStreamAsync();
        }
        catch (Exception ex)
        {
            return new Error("failed to get calendar data", ex);
        }
    }

}
