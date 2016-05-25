﻿using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.ComponentModel;
using System.Globalization;

namespace engenious.Content.Pipeline
{
    [Serializable()]
    public class ProcessorSettings
    {
        public ProcessorSettings()
        {
        }

        #region Serialization
        private void setPrimitive(PropertyDescriptor property,object obj,string val)
        {
            var code = Type.GetTypeCode(property.PropertyType);
            if (code != TypeCode.String && val == null)
                return;
            switch(code)
            {
                case TypeCode.String:
                    property.SetValue(obj,val);
                    break;
                case TypeCode.Char:
                    property.SetValue(obj,string.IsNullOrEmpty(val) ? '\0' : val[0]);
                    break;
                case TypeCode.SByte:
                    {
                        sbyte num;
                        if (sbyte.TryParse(val,NumberStyles.Integer,CultureInfo.InvariantCulture,out num))
                        {
                            property.SetValue(obj,num);
                        }
                    }
                    break;
                case TypeCode.Int16:
                    {
                        short num;
                        if (short.TryParse(val,NumberStyles.Integer,CultureInfo.InvariantCulture,out num))
                        {
                            property.SetValue(obj,num);
                        }
                    }
                    break;
                case TypeCode.Int32:
                    {
                        int num;
                        if (int.TryParse(val,NumberStyles.Integer,CultureInfo.InvariantCulture,out num))
                        {
                            property.SetValue(obj,num);
                        }
                    }
                    break;
                case TypeCode.Int64:
                    {
                        long num;
                        if (long.TryParse(val,NumberStyles.Integer,CultureInfo.InvariantCulture,out num))
                        {
                            property.SetValue(obj,num);
                        }
                    }
                    break;
                case TypeCode.Byte:
                    {
                        byte num;
                        if (byte.TryParse(val,NumberStyles.Integer,CultureInfo.InvariantCulture,out num))
                        {
                            property.SetValue(obj,num);
                        }
                    }
                    break;
                case TypeCode.UInt16:
                    {
                        ushort num;
                        if (ushort.TryParse(val,NumberStyles.Integer,CultureInfo.InvariantCulture,out num))
                        {
                            property.SetValue(obj,num);
                        }
                    }
                    break;
                case TypeCode.UInt32:
                    {
                        uint num;
                        if (uint.TryParse(val,NumberStyles.Integer,CultureInfo.InvariantCulture,out num))
                        {
                            property.SetValue(obj,num);
                        }
                    }
                    break;
                case TypeCode.UInt64:
                    {
                        ulong num;
                        if (ulong.TryParse(val,NumberStyles.Integer,CultureInfo.InvariantCulture,out num))
                        {
                            property.SetValue(obj,num);
                        }
                    }
                    break;
                case TypeCode.Boolean:
                    {
                        bool tmp;
                        if (bool.TryParse(val,out tmp))
                            property.SetValue(obj,tmp);
                    }
                    break;
                case TypeCode.DateTime:
                    {
                        DateTime dt;
                        if (DateTime.TryParse(val,out dt))
                            property.SetValue(obj,dt);
                    }
                    break;
                case TypeCode.Single:
                    {
                        float num;
                        if (float.TryParse(val,NumberStyles.Float,CultureInfo.InvariantCulture.NumberFormat,out num))
                        {
                            property.SetValue(obj,num);
                        }
                    }
                    break;
                case TypeCode.Double:
                    {
                        double num;
                        if (double.TryParse(val,NumberStyles.Float,CultureInfo.InvariantCulture.NumberFormat,out num))
                        {
                            property.SetValue(obj,num);
                        }
                    }
                    break;
                case TypeCode.Decimal:
                    {
                        decimal num;
                        if (decimal.TryParse(val,NumberStyles.Float,CultureInfo.InvariantCulture.NumberFormat,out num))
                        {
                            property.SetValue(obj,num);
                        }
                    }
                    break;
            }
        }
        private void ReadObject(XmlNodeList nodes,object obj)
        {
            var props = TypeDescriptor.GetProperties(obj).OfType<PropertyDescriptor>().ToDictionary(x => x.Name, x => x);
            foreach (var setting in nodes.OfType<XmlElement>())
            {
                PropertyDescriptor property;
                if (props.TryGetValue(setting.Name, out property))
                {
                    var val = setting.ChildNodes.OfType<XmlText>().FirstOrDefault()?.InnerText;
                    if (property.PropertyType.IsPrimitive)
                    {
                        setPrimitive(property,obj,val);
                    }
                    else if (property.PropertyType.IsEnum)
                    {
                        try{
                            property.SetValue(obj,Enum.Parse(property.PropertyType,val));
                        }catch{}
                    }
                    else
                    {
                        var tmp =Activator.CreateInstance(property.PropertyType);
                        ReadObject(setting.ChildNodes,tmp);
                        property.SetValue(obj,tmp);
                    }
                }
            }
        }

        public virtual void Read(XmlNodeList nodes)
        {
            ReadObject(nodes,this);
        }

        private void WriteObject(XmlWriter writer, object obj)
        {
            foreach (var prop in TypeDescriptor.GetProperties(obj).OfType<PropertyDescriptor>())
            {
                var type = prop.PropertyType;
                if (prop.IsReadOnly)
                    continue;
                if (type.IsPrimitive || type.IsEnum)
                {
                    writer.WriteElementString(prop.Name, prop.GetValue(obj).ToString());
                }
                else
                {
                    writer.WriteStartElement(prop.Name);
                    WriteObject(writer, prop.GetValue(obj));
                    writer.WriteEndElement();
                }
            }
        }

        public virtual void Write(XmlWriter writer)
        {
            WriteObject(writer, this);
        }

        #endregion
    }
}

