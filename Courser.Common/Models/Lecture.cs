using System;

namespace Courser.Common.Models;

public sealed class Lecture
{
    public required string Name { get; init; }
    public required string Rooms { get; init; }
    public required DateTimeOffset Start { get; init; }
    public required DateTimeOffset End { get; init; }
}
