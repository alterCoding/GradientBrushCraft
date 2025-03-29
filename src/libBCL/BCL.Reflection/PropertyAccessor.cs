using System;
using System.Linq.Expressions;

namespace AltCoD.BCL.Reflection
{
    /// <summary>
    /// Helper that wraps access to a property (get/set) with compiled lambda expression (note: far better than reflection)
    /// </summary>
    /// <typeparam name="TObj"></typeparam>
    public static class PropertyAccessor<TObj>
    {
        public static Func<TObj, TProp> Getter<TProp>(string propertyName)
        {
            var target = Expression.Parameter(typeof(TObj), "value");
            var expr = Expression.Property(target, propertyName);

            var result = Expression.Lambda<Func<TObj, TProp>>(expr, target).Compile();

            return result;
        }

        public static Action<TObj, TProp> Setter<TProp>(string propertyName)
        {
            var target = Expression.Parameter(typeof(TObj));
            var value = Expression.Parameter(typeof(TProp), propertyName);
            var expr = Expression.Property(target, propertyName);

            var result = Expression.Lambda<Action<TObj, TProp>>(Expression.Assign(expr, value), target, value).Compile();

            return result;
        }
    }
}
