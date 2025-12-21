using System;

namespace CustomPackages.Package.Extensions
{
    public class NotifiedProperty<T> : IProperty<T>
    {
        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                OnValueChanged?.Invoke(value);
            }
        }

        public event Action<T> OnValueChanged;

        public void CleanUp() 
            => OnValueChanged = null;

        private T _value;
        
        public static implicit operator T(NotifiedProperty<T> notifiedProperty)
        {
            return notifiedProperty.Value;
        }
    }

    public interface IProperty<T> : IReadonlyProperty<T>, IValueSetter<T>
    {
        new T Value { get; set; }
    }

    public interface IReadonlyProperty<T> : IValueGetter<T>, IValueNotifier<T>
    {
    }

    public interface IValueGetter<out T>
    {
        T Value { get; }
    }

    public interface IValueSetter<in T>
    {
        T Value { set; }
    }

    public interface IValueNotifier<out T>
    {
        event Action<T> OnValueChanged;
    }
}