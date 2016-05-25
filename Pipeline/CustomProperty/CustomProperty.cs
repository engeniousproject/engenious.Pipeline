using System;

namespace engenious.Pipeline
{
    public class CustomProperty
    {
        public delegate void SetCustomProperty(object value);
        private SetCustomProperty setter;
        public CustomProperty(string name, object value, Type type, bool readOnly, bool visible,SetCustomProperty setter)
        {
            this.Name = name;
            this.Value = value;
            this.Type = type;
            this.ReadOnly = readOnly;
            this.Visible = visible;
            this.setter = setter;
        }

        public string Name{get;private set;}
        private object value;
        public object Value{
            get{
                return value;
            }
            internal set{
                this.value = value;
                setter(value);
            }
            }
        public Type Type{get;private set;}
        public bool ReadOnly{get;private set;}
        public bool Visible{get;private set;}
    }
}

