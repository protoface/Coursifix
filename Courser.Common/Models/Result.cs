using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Courser.Common.Models;

public readonly union Result<T>(T, Error) where T : notnull;
public readonly union Maybe<T>(T?, Error);

static class ResultExtensions
{
    extension<T>(Maybe<T>)
    {
        public static Maybe<T> Empty => default;
    }
}

public sealed class Error
{
    public Error(string message, Exception? ex = null, [CallerMemberName] string memberName = "")
    {
        Message = message;
        Ex = ex;
        MemberName = memberName;
#if DEBUG
        StackTrace = new(1);
#endif
    }

    public Error(Exception ex, [CallerMemberName] string memberName = "") : this(ex.Message, ex, memberName) { }

    public string Message { get; }
    public Exception? Ex { get; }
    public string MemberName { get; }
    public StackTrace? StackTrace { get; }
}