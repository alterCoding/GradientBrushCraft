using System;
using System.Reflection;

namespace AltCoD.BCL.Reflection
{
    /// <summary>
    /// </summary>
    /// @internal CustomAttributeExtensions has been introduced with netfx 4.5 but is of weak added-value and adds an 
    /// useless dependency (in the context of .net legacy support purpose)
    //
    public static class CustomAttributeHelper
    {
        /// <summary>
        /// same as <see cref="CustomAttributeExtensions"/> but remove the netfx 4.5 dependency
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static T GetCustomAttribute<T>(this Assembly assembly) where T:Attribute
        {
            return (T)Attribute.GetCustomAttribute(assembly, typeof(T));
        }

        /// <summary>
        /// Get the 1th custom attribute (if any)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assembly"></param>
        /// <param name="attrib"></param>
        /// <returns></returns>
        public static bool TryCustomAttribute<T>(this Assembly assembly, out T attrib) where T:Attribute
        {
            var attributes = Attribute.GetCustomAttributes(assembly, typeof(T));
            if(attributes.Length > 0)
            {
                attrib = attributes[0] as T;
                return true;
            }
            else
            {
                attrib = null;
                return false;
            }
        }
    }
}
