using System;
using Courser.Common.Models;

namespace Courser.Common.Interfaces;

public interface ICalendarService
{
    // public Task<Result<Lecture?>> GetCurrentLectureAsync();
    public Task<Result<Lecture?>> GetNextEndingLectureAsync();
}
