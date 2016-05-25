using System;
using System.ComponentModel;

namespace engenious.Pipeline
{
    public class CustomPropertyDescriptor : PropertyDescriptor
    {
        private object component;
        private PropertyDescriptor property;
        public CustomPropertyDescriptor(PropertyDescriptor property,object component,Attribute [] attrs) :base(property.Name, attrs)
        {
            this.component = component;
            this.property = property;
        }

        #region PropertyDescriptor specific

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override Type ComponentType
        {
            get{
                return property.ComponentType;
            }
        }

        public override object GetValue(object component)
        {
            return property.GetValue(this.component);
        }

        public override string Description
        {
            get { return property.Description; }
        }

        public override string Category
        {
            get { return property.Category; }
        }

        public override string DisplayName
        {
            get { return property.DisplayName; }
        }

        public override bool IsReadOnly
        {
            get { return property.IsReadOnly; }
        }

        public override void ResetValue(object component)
        {
            //Have to implement
            property.ResetValue(this.component);
        }

        public override bool ShouldSerializeValue(object component)
        {
            return property.ShouldSerializeValue(this.component);
        }

        public override void SetValue(object component, object value)
        {
            property.SetValue(this.component,value);
        }

        public override Type PropertyType
        {
            get { return property.PropertyType; }
        }

        #endregion
    }
}

