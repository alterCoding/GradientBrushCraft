using System;
using System.Drawing;
using System.ComponentModel;

namespace AltCoD.GradientCraft
{
    using UI.WinForms;

    public interface IGradientChangesSource : ISynchronizeInvoke
    {
        event EventHandler<GradientChangeEvent> OnGradientChange;

        TextBoxContent GetExplanation();
    }

    public class GradientChangeEvent : EventArgs
    {
        public enum EventType 
        { 
            undefined,
            gradientType,
            addRemoveProperty,
            propertyValue
        }

        public static GradientChangeEvent OfType(Brush brush)
        {
            return new GradientChangeEvent(EventType.gradientType)
            {
                TypeName = brush.GetType()
            };
        }
        public static GradientChangeEvent PropertyValue<TProp>(Brush brush, string propname, TProp _)
        {
            return new GradientChangeEvent(EventType.propertyValue)
            {
                TypeName = typeof(TProp),
                PropertyName = $"{brush.GetType().Name}.{propname}"
            };
        }
        public static GradientChangeEvent PropertyValue<TProp>(string propname, TProp _)
        {
            return new GradientChangeEvent(EventType.propertyValue)
            {
                TypeName = typeof(TProp),
                PropertyName = propname
            };
        }
        public static GradientChangeEvent Property<TProp>(Brush brush, string propname)
        {
            return new GradientChangeEvent(EventType.addRemoveProperty)
            {
                TypeName = typeof(TProp),
                PropertyName = $"{brush.GetType().Name}.{propname}"
            };
        }

        private GradientChangeEvent(EventType type)
        {
            ChangeType = type;
        }

        public EventType ChangeType { get; private set; }

        public Type TypeName { get; private set; }

        public string PropertyName { get; private set; } = string.Empty;
    }
}
