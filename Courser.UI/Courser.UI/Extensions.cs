using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Courser.Common.Models;
using Courser.UI.Utils;
using Eto.Drawing;
using Eto.Forms;

namespace Courser.UI;

internal static class Extensions
{
    private static Dictionary<Type, Delegate> updateFunctions { get; set; } = [];

    extension<T>(T obj)
    {
        public T Apply(Action<T> action)
        {
            action(obj);
            return obj;
        }
    }


    extension<T>(T) where T : Command, new()
    {
        public static T NewTyped<TObj>(Action<TObj, EventArgs> handler)
        {
            var command = new T();
            command.Executed += (s, e) => handler((TObj)s!, e);
            return command;
        }
    }

    extension<T>(T widget) where T : BindableWidget
    {
#pragma warning disable IDE0060 // Remove unused parameter
        public T Bind<TBindable, TModel, TValue>(Form<TModel> parent, Func<T, BindableBinding<TBindable, TValue>> bind,
            Expression<Func<TModel, TValue>> expression) where TModel : ViewModel<TModel>, new()
            where TBindable : IBindable
#pragma warning restore IDE0060 // Remove unused parameter
        {
            bind(widget).BindDataContext(Binding.Property(expression));
            return widget;
        }
    }

    extension<T>(EventHandler<T> eventHandler)
    {
        public void Add(Func<Task> taskFunc) => eventHandler += (_, _) => taskFunc();
    }

    extension(Lecture lecture)
    {
        public Icon Icon => Icons.GetLectureIcon(lecture.Name);
    }

    extension<T>(T viewModel) where T : ViewModel<T>, new()
    {
        public void Update(T updateModel)
        {
            if (!updateFunctions.TryGetValue(typeof(T), out var updateFunc))
            {
                ParameterExpression src = Expression.Parameter(typeof(T), "src"), dst = Expression.Parameter(typeof(T), "dst");
                updateFunc = Expression.Lambda<Action<T, T>>(Expression.Block(typeof(T).GetProperties().Select(prop => Expression.Assign(Expression.Property(dst, prop), Expression.Property(src, prop)))), dst, src).Compile();
                updateFunctions.Add(typeof(T), updateFunc);
            }
            ((Action<T, T>)updateFunc)(viewModel, updateModel);
        }
    }
}

internal abstract class ViewModel<TModel> : INotifyPropertyChanged where TModel : ViewModel<TModel>, new()
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void SetAndRaise<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;

        field = value;
        PropertyChanged?.Invoke(this, new(propertyName));
    }

}
