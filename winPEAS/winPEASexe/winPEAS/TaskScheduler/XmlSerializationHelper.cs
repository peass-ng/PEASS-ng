using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace winPEAS.TaskScheduler
{
    internal static class XmlSerializationHelper
    {
        public static object GetDefaultValue([NotNull] PropertyInfo prop)
        {
            var attributes = prop.GetCustomAttributes(typeof(DefaultValueAttribute), true);
            if (attributes.Length > 0)
            {
                var defaultAttr = (DefaultValueAttribute)attributes[0];
                return defaultAttr.Value;
            }

            // Attribute not found, fall back to default value for the type 
            if (prop.PropertyType.IsValueType)
                return Activator.CreateInstance(prop.PropertyType);
            return null;
        }

        private static bool GetPropertyValue(object obj, [NotNull] string property, ref object outVal)
        {
            PropertyInfo pi = obj?.GetType().GetProperty(property);
            if (pi != null)
            {
                outVal = pi.GetValue(obj, null);
                return true;
            }
            return false;
        }

        private static bool GetAttributeValue(Type objType, Type attrType, string property, bool inherit, ref object outVal)
        {
            object[] attrs = objType.GetCustomAttributes(attrType, inherit);
            if (attrs.Length > 0)
                return GetPropertyValue(attrs[0], property, ref outVal);
            return false;
        }

        private static bool GetAttributeValue([NotNull] PropertyInfo propInfo, Type attrType, string property, bool inherit, ref object outVal)
        {
            Attribute attr = Attribute.GetCustomAttribute(propInfo, attrType, inherit);
            return GetPropertyValue(attr, property, ref outVal);
        }

        private static bool IsStandardType(Type type) => type.IsPrimitive || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(Decimal) || type == typeof(Guid) || type == typeof(TimeSpan) || type == typeof(string) || type.IsEnum;

        private static bool HasMembers([NotNull] object obj)
        {
            if (obj is IXmlSerializable)
            {
                using (System.IO.MemoryStream mem = new System.IO.MemoryStream())
                {
                    using (XmlTextWriter tw = new XmlTextWriter(mem, Encoding.UTF8))
                    {
                        ((IXmlSerializable)obj).WriteXml(tw);
                        tw.Flush();
                        return mem.Length > 3;
                    }
                }
            }

            // Enumerate each public property
            PropertyInfo[] props = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var pi in props)
            {
                if (!Attribute.IsDefined(pi, typeof(XmlIgnoreAttribute), false))
                {
                    object value = pi.GetValue(obj, null);
                    if (!Equals(value, GetDefaultValue(pi)))
                    {
                        if (!IsStandardType(pi.PropertyType))
                        {
                            if (HasMembers(value))
                                return true;
                        }
                        else
                            return true;
                    }
                }
            }
            return false;
        }

        public static string GetPropertyAttributeName([NotNull] PropertyInfo pi)
        {
            object oVal = null;
            string eName = pi.Name;
            if (GetAttributeValue(pi, typeof(XmlAttributeAttribute), "AttributeName", false, ref oVal))
                eName = oVal.ToString();
            return eName;
        }

        public static string GetPropertyElementName([NotNull] PropertyInfo pi)
        {
            object oVal = null;
            string eName = pi.Name;
            if (GetAttributeValue(pi, typeof(XmlElementAttribute), "ElementName", false, ref oVal))
                eName = oVal.ToString();
            else if (GetAttributeValue(pi.PropertyType, typeof(XmlRootAttribute), "ElementName", true, ref oVal))
                eName = oVal.ToString();
            return eName;
        }

        public delegate bool PropertyConversionHandler([NotNull] PropertyInfo pi, Object obj, ref Object value);

        public static bool WriteProperty([NotNull] XmlWriter writer, [NotNull] PropertyInfo pi, [NotNull] Object obj, PropertyConversionHandler handler = null)
        {
            if (Attribute.IsDefined(pi, typeof(XmlIgnoreAttribute), false) || Attribute.IsDefined(pi, typeof(XmlAttributeAttribute), false))
                return false;

            object value = pi.GetValue(obj, null);
            object defValue = GetDefaultValue(pi);
            if ((value == null && defValue == null) || (value != null && value.Equals(defValue)))
                return false;

            Type propType = pi.PropertyType;
            if (handler != null && handler(pi, obj, ref value))
                propType = value.GetType();

            bool isStdType = IsStandardType(propType);
            bool rw = pi.CanRead && pi.CanWrite;
            bool ro = pi.CanRead && !pi.CanWrite;
            string eName = GetPropertyElementName(pi);
            if (isStdType && rw)
            {
                string output = GetXmlValue(value, propType);
                if (output != null)
                    writer.WriteElementString(eName, output);
            }
            else if (!isStdType)
            {
                object outVal = null;
                if (propType.GetInterface("IXmlSerializable") == null && GetAttributeValue(pi, typeof(XmlArrayAttribute), "ElementName", true, ref outVal) && propType.GetInterface("IEnumerable") != null)
                {
                    if (string.IsNullOrEmpty(outVal.ToString())) outVal = eName;
                    writer.WriteStartElement(outVal.ToString());
                    var attributes = Attribute.GetCustomAttributes(pi, typeof(XmlArrayItemAttribute), true);
                    var dict = new Dictionary<Type, string>(attributes.Length);
                    foreach (XmlArrayItemAttribute a in attributes)
                        dict.Add(a.Type, a.ElementName);
                    foreach (object item in ((System.Collections.IEnumerable)value))
                    {
                        string aeName;
                        Type itemType = item.GetType();
                        if (dict.TryGetValue(itemType, out aeName))
                        {
                            if (IsStandardType(itemType))
                                writer.WriteElementString(aeName, GetXmlValue(item, itemType));
                            else
                                WriteObject(writer, item, null, false, aeName);
                        }
                    }
                    writer.WriteEndElement();
                }
                else
                    WriteObject(writer, value);
            }
            return false;
        }

        private static string GetXmlValue([NotNull] object value, Type propType)
        {
            string output = null;
            if (propType.IsEnum)
            {
                if (Attribute.IsDefined(propType, typeof(FlagsAttribute), false))
                    output = Convert.ChangeType(value, Enum.GetUnderlyingType(propType)).ToString();
                else
                    output = value.ToString();
            }
            else
            {
                switch (propType.FullName)
                {
                    case "System.Boolean":
                        output = XmlConvert.ToString((System.Boolean)value);
                        break;
                    case "System.Byte":
                        output = XmlConvert.ToString((System.Byte)value);
                        break;
                    case "System.Char":
                        output = XmlConvert.ToString((System.Char)value);
                        break;
                    case "System.DateTime":
                        output = XmlConvert.ToString((System.DateTime)value, XmlDateTimeSerializationMode.RoundtripKind);
                        break;
                    case "System.DateTimeOffset":
                        output = XmlConvert.ToString((System.DateTimeOffset)value);
                        break;
                    case "System.Decimal":
                        output = XmlConvert.ToString((System.Decimal)value);
                        break;
                    case "System.Double":
                        output = XmlConvert.ToString((System.Double)value);
                        break;
                    case "System.Single":
                        output = XmlConvert.ToString((System.Single)value);
                        break;
                    case "System.Guid":
                        output = XmlConvert.ToString((System.Guid)value);
                        break;
                    case "System.Int16":
                        output = XmlConvert.ToString((System.Int16)value);
                        break;
                    case "System.Int32":
                        output = XmlConvert.ToString((System.Int32)value);
                        break;
                    case "System.Int64":
                        output = XmlConvert.ToString((System.Int64)value);
                        break;
                    case "System.SByte":
                        output = XmlConvert.ToString((System.SByte)value);
                        break;
                    case "System.TimeSpan":
                        output = XmlConvert.ToString((System.TimeSpan)value);
                        break;
                    case "System.UInt16":
                        output = XmlConvert.ToString((System.UInt16)value);
                        break;
                    case "System.UInt32":
                        output = XmlConvert.ToString((System.UInt32)value);
                        break;
                    case "System.UInt64":
                        output = XmlConvert.ToString((System.UInt64)value);
                        break;
                    default:
                        output = value == null ? string.Empty : value.ToString();
                        break;
                }
            }

            return output;
        }

        public static void WriteObjectAttributes([NotNull] XmlWriter writer, [NotNull] object obj, PropertyConversionHandler handler = null)
        {
            // Enumerate each property
            foreach (var pi in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                if (Attribute.IsDefined(pi, typeof(XmlAttributeAttribute), false))
                    WriteObjectAttribute(writer, pi, obj, handler);
        }

        public static void WriteObjectAttribute([NotNull] XmlWriter writer, [NotNull] PropertyInfo pi, [NotNull] object obj, PropertyConversionHandler handler = null)
        {
            object value = pi.GetValue(obj, null);
            object defValue = GetDefaultValue(pi);
            if ((value == null && defValue == null) || (value != null && value.Equals(defValue)))
                return;

            Type propType = pi.PropertyType;
            if (handler != null && handler(pi, obj, ref value))
                propType = value.GetType();

            writer.WriteAttributeString(GetPropertyAttributeName(pi), GetXmlValue(value, propType));
        }

        public static void WriteObjectProperties([NotNull] XmlWriter writer, [NotNull] object obj, PropertyConversionHandler handler = null)
        {
            // Enumerate each public property
            foreach (var pi in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                WriteProperty(writer, pi, obj, handler);
        }

        public static void WriteObject([NotNull] XmlWriter writer, [NotNull] object obj, PropertyConversionHandler handler = null, bool includeNS = false, string elemName = null)
        {
            if (obj == null)
                return;

            // Get name of top level element
            string oName = elemName ?? GetElementName(obj);

            if (!HasMembers(obj))
                return;

            if (includeNS)
                writer.WriteStartElement(oName, GetTopLevelNamespace(obj));
            else
                writer.WriteStartElement(oName);

            if (obj is IXmlSerializable)
            {
                ((IXmlSerializable)obj).WriteXml(writer);
            }
            else
            {
                WriteObjectAttributes(writer, obj, handler);
                WriteObjectProperties(writer, obj, handler);
            }

            writer.WriteEndElement();
        }

        public static string GetElementName([NotNull] object obj)
        {
            object oVal = null;
            return GetAttributeValue(obj.GetType(), typeof(XmlRootAttribute), "ElementName", true, ref oVal) ? oVal.ToString() : obj.GetType().Name;
        }

        public static string GetTopLevelNamespace([NotNull] object obj)
        {
            object oVal = null;
            return GetAttributeValue(obj.GetType(), typeof(XmlRootAttribute), "Namespace", true, ref oVal) ? oVal.ToString() : null;
        }

        public static void ReadObjectProperties([NotNull] XmlReader reader, [NotNull] object obj, PropertyConversionHandler handler = null)
        {
            // Build property lookup table
            PropertyInfo[] props = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            Dictionary<string, PropertyInfo> attrHash = new Dictionary<string, PropertyInfo>(props.Length);
            Dictionary<string, PropertyInfo> propHash = new Dictionary<string, PropertyInfo>(props.Length);
            foreach (var pi in props)
            {
                if (!Attribute.IsDefined(pi, typeof(XmlIgnoreAttribute), false))
                {
                    if (Attribute.IsDefined(pi, typeof(XmlAttributeAttribute), false))
                        attrHash.Add(GetPropertyAttributeName(pi), pi);
                    else
                        propHash.Add(GetPropertyElementName(pi), pi);
                }
            }

            if (reader.HasAttributes)
            {
                for (int i = 0; i < reader.AttributeCount; i++)
                {
                    PropertyInfo pi;
                    reader.MoveToAttribute(i);
                    if (attrHash.TryGetValue(reader.LocalName, out pi))
                    {
                        if (IsStandardType(pi.PropertyType))
                        {
                            object value = null;
                            if (pi.PropertyType.IsEnum)
                                value = Enum.Parse(pi.PropertyType, reader.Value);
                            else
                                value = Convert.ChangeType(reader.Value, pi.PropertyType);

                            if (handler != null)
                                handler(pi, obj, ref value);

                            pi.SetValue(obj, value, null);
                        }
                    }
                }
            }

            while (reader.MoveToContent() == XmlNodeType.Element)
            {
                PropertyInfo pi;
                object outVal = null;
                if (propHash.TryGetValue(reader.LocalName, out pi))
                {
                    var tc = TypeDescriptor.GetConverter(pi.PropertyType);
                    if (IsStandardType(pi.PropertyType))
                    {
                        object value = null;
                        if (pi.PropertyType.IsEnum)
                            value = Enum.Parse(pi.PropertyType, reader.ReadElementContentAsString());
                        else if (pi.PropertyType == typeof(Guid))
                            value = new GuidConverter().ConvertFromString(reader.ReadElementContentAsString());
                        else
                            value = reader.ReadElementContentAs(pi.PropertyType, null);

                        if (handler != null)
                            handler(pi, obj, ref value);

                        pi.SetValue(obj, value, null);
                    }
                    else if (pi.PropertyType == typeof(Version))
                    {
                        Version v = new Version(reader.ReadElementContentAsString());
                        pi.SetValue(obj, v, null);
                    }
                    else if (pi.PropertyType.GetInterface("IEnumerable") != null && pi.PropertyType.GetInterface("IXmlSerializable") == null && GetAttributeValue(pi, typeof(XmlArrayAttribute), "ElementName", true, ref outVal))
                    {
                        string elem = string.IsNullOrEmpty(outVal?.ToString()) ? pi.Name : outVal.ToString();
                        reader.ReadStartElement(elem);
                        var attributes = Attribute.GetCustomAttributes(pi, typeof(XmlArrayItemAttribute), true);
                        var dict = new Dictionary<string, Type>(attributes.Length);
                        foreach (XmlArrayItemAttribute a in attributes)
                            dict.Add(a.ElementName, a.Type);
                        List<object> output = new List<object>();
                        while (reader.MoveToContent() == XmlNodeType.Element)
                        {
                            Type itemType;
                            if (dict.TryGetValue(reader.LocalName, out itemType))
                            {
                                object o;
                                if (IsStandardType(itemType))
                                    o = reader.ReadElementContentAs(itemType, null);
                                else
                                {
                                    o = Activator.CreateInstance(itemType);
                                    ReadObject(reader, o, handler);
                                }
                                if (o != null)
                                    output.Add(o);
                            }
                        }
                        reader.ReadEndElement();
                        if (output.Count > 0)
                        {
                            System.Collections.IEnumerable par = output;
                            Type et = typeof(object);
                            if (dict.Count == 1)
                            {
                                foreach (var v in dict.Values) { et = v; break; }
                            }
                            /*else
							{
								Type t1 = output[0].GetType();
								bool same = true;
								foreach (var item in output)
									if (item.GetType() != t1) { same = false; break; }
								if (same)
									et = t1;
							}
							if (et != typeof(object))
							{
								Array ao = Array.CreateInstance(et, output.Count);
								for (int i = 0; i < output.Count; i++)
									ao.SetValue(output[i], i);
								par = ao;
							}
							else
								par = output.ToArray();*/
                            bool done = false;
                            if (pi.PropertyType == par.GetType() || (pi.PropertyType.IsArray && (pi.PropertyType.GetElementType() == typeof(object) || pi.PropertyType.GetElementType() == et)))
                                try { pi.SetValue(obj, par, null); done = true; } catch { }
                            if (!done)
                            {
                                var mi = pi.PropertyType.GetMethod("AddRange", new Type[] { typeof(System.Collections.IEnumerable) });
                                if (mi != null)
                                    try { mi.Invoke(pi.GetValue(obj, null), new object[] { par }); done = true; } catch { }
                            }
                            if (!done)
                            {
                                var mi = pi.PropertyType.GetMethod("Add", new Type[] { typeof(object) });
                                if (mi != null)
                                    try { foreach (var i in par) mi.Invoke(pi.GetValue(obj, null), new object[] { i }); done = true; } catch { }
                            }
                            if (!done && et != typeof(Object))
                            {
                                var mi = pi.PropertyType.GetMethod("Add", new Type[] { et });
                                if (mi != null)
                                    try { foreach (var i in par) mi.Invoke(pi.GetValue(obj, null), new object[] { i }); done = true; } catch { }
                            }
                            // Throw error if not done
                        }
                    }
                    else
                    {
                        object inst = pi.GetValue(obj, null) ?? Activator.CreateInstance(pi.PropertyType);
                        if (inst == null)
                            throw new InvalidOperationException($"Can't get instance of {pi.PropertyType.Name}.");
                        ReadObject(reader, inst, handler);
                    }
                }
                else
                {
                    reader.Skip();
                    reader.MoveToContent();
                }
            }
        }

        public static void ReadObject([NotNull] XmlReader reader, [NotNull] object obj, PropertyConversionHandler handler = null)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            reader.MoveToContent();

            if (obj is IXmlSerializable)
            {
                ((IXmlSerializable)obj).ReadXml(reader);
            }
            else
            {
                object oVal = null;
                string oName = GetAttributeValue(obj.GetType(), typeof(XmlRootAttribute), "ElementName", true, ref oVal) ? oVal.ToString() : obj.GetType().Name;
                if (reader.LocalName != oName)
                    throw new XmlException("XML element name does not match object.");

                if (!reader.IsEmptyElement)
                {
                    reader.ReadStartElement();
                    reader.MoveToContent();
                    ReadObjectProperties(reader, obj, handler);
                    reader.ReadEndElement();
                }
                else
                    reader.Skip();
            }
        }

        public static void ReadObjectFromXmlText([NotNull] string xml, [NotNull] object obj, PropertyConversionHandler handler = null)
        {
            using (System.IO.StringReader sr = new System.IO.StringReader(xml))
            {
                using (XmlReader reader = XmlReader.Create(sr))
                {
                    reader.MoveToContent();
                    ReadObject(reader, obj, handler);
                }
            }
        }

        public static string WriteObjectToXmlText([NotNull] object obj, PropertyConversionHandler handler = null)
        {
            StringBuilder sb = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings() { Indent = true }))
                WriteObject(writer, obj, handler, true);
            return sb.ToString();
        }
    }
}
