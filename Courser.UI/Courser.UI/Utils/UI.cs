using System;
using System.Threading.Tasks;
using Eto.Forms;

namespace Courser.UI.Utils;

public static class UI
{
    public static void Invoke(Action action) => Application.Instance.Invoke(action);
    public static T Get<T>(Func<T> func) => Application.Instance.Invoke(func);

    public static Task InvokeAsync(Action action) => Application.Instance.InvokeAsync(action);
    public static Task<T> GetAsync<T>(Func<T> func) => Application.Instance.InvokeAsync(func);
}
