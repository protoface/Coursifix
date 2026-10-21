using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Eto.Forms;

namespace Courser.UI;

internal static class Extensions
{
    extension<T>(T obj)
    {
        public T Apply(Action<T> action)
        {
            action(obj);
            return obj;
        }
    }

    extension<T>(T widget) where T : BindableWidget
    {
        public T Bind<TBindable, TModel, TValue>(Func<T, BindableBinding<TBindable, TValue>> bind, Expression<Func<TModel, TValue>> expression) where TBindable : IBindable where TModel : ViewModel<TModel>
        {
            bind(widget).BindDataContext(Binding.Property(expression));
            return widget;
        }
    }

    extension<T>(EventHandler<T> eventHandler)
    {
        public void Add(Func<Task> taskFunc) => eventHandler += (_, _) => taskFunc();
    }

}

abstract class ViewModel<TModel> : INotifyPropertyChanged where TModel : ViewModel<TModel>
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void SetAndRaise<T>(ref T field, T value, [CallerMemberName] string propertyName = "") where T : class
    {
        if (field != value)
        {
            field = value;
            PropertyChanged?.Invoke(this, new(propertyName));
        }
    }

}