using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Collections;
using System.Runtime.InteropServices;

namespace System.Yaml.Serialization
{
    /// <summary>
    /// Converts C# object to YamlNode
    /// </summary>
    /// <example>
    /// <code>
    /// object obj;
    /// YamlNode node = YamlRepresenter.ObjectToNode(obj);
    /// </code>
    /// </example>
    internal class YamlRepresenter: YamlNodeManipulator
    {
        private static string TypeNameToYamlTag(Type type)
        {
            /*
            if ( TypeUtils.GetType(type.FullName) == null ) {
                throw new ArgumentException(
                    "Can not serialize (non public?) type '{0}'.".DoFormat(type.FullName));
            }
            */
            if ( type == typeof(int) )
                return YamlNode.ExpandTag("!!int");
            if ( type == typeof(string) )
                return YamlNode.ExpandTag("!!str");
            if ( type == typeof(Double) )
                return YamlNode.ExpandTag("!!float");
            if ( type == typeof(bool) )
                return YamlNode.ExpandTag("!!bool");
            if ( type == typeof(object[]) )
                return YamlNode.ExpandTag("!!seq");
            return "!" + type.FullName;
        }

        public YamlNode ObjectToNode(object obj)
        {
            return ObjectToNode(obj, YamlNode.DefaultConfig);
        }

        YamlConfig config;
        public YamlNode ObjectToNode(object obj, YamlConfig config)
        {
            this.config = config;
            appeared.Clear();
            if ( config.OmitTagForRootNode ) {
                return ObjectToNode(obj, obj.GetType());
            } else {
                return ObjectToNode(obj, (Type)null);
            }
        }

        YamlNode ObjectToNode(object obj, Type expect)
        {
            if ( obj != null && obj.GetType().IsClass && ( !(obj is string) || ((string)obj).Length >= 1000 ) )
                if ( appeared.ContainsKey(obj) )
                    return appeared[obj];

            var node = ObjectToNodeSub(obj, expect);

            if ( expect != null && expect != typeof(Object) )
                node.Properties["expectedTag"] = TypeNameToYamlTag(expect);

            AppendToAppeared(obj, node);

            return node;
        }

        private void AppendToAppeared(object obj, YamlNode node)
        {
            if ( obj != null && obj.GetType().IsClass && ( !( obj is string ) || ( (string)obj ).Length >= 1000 ) )
                if ( !appeared.ContainsKey(obj) )
                    appeared.Add(obj, node);
        }
        Dictionary<object, YamlNode> appeared = 
            new Dictionary<object, YamlNode>(TypeUtils.EqualityComparerByRef<object>.Default);

        YamlNode ObjectToNodeSub(object obj, Type expect)
        {
            // !!null
            if ( obj == null )
                return str("!!null", "null");

            YamlScalar node;
            if ( config.TagResolver.Encode(obj, out node) )
                return node;

            var type = obj.GetType();

            if ( obj is IntPtr || type.IsPointer )
                throw new ArgumentException("Pointer object '{0}' can not be serialized.".DoFormat(obj.ToString()));

            if ( obj is char ) {
                // config.TypeConverter.ConvertToString("\0") does not show "\0"
                var n = str(TypeNameToYamlTag(type), obj.ToString() );
                return n;
            }

            // bool, byte, sbyte, decimal, double, float, int ,uint, long, ulong, short, ushort, string, enum
            if ( type.IsPrimitive || type.IsEnum || type == typeof(decimal) || type == typeof(string) ) {
                var n = str(TypeNameToYamlTag(type), config.TypeConverter.ConvertToString(obj) );
                return n;
            }

            // TypeConverterAttribute 
            if ( EasyTypeConverter.IsTypeConverterSpecified(type) )
                return str(TypeNameToYamlTag(type), config.TypeConverter.ConvertToString(obj));

            // array
            if ( type.IsArray ) 
                return CreateArrayNode((Array)obj);

            if ( type == typeof(Dictionary<object, object>) )
                return DictionaryToMap(obj);

            // class / struct
            if ( type.IsClass || type.IsValueType )
                return CreateMapping(TypeNameToYamlTag(type), obj);

            throw new NotImplementedException(
                "Type '{0}' could not be written".DoFormat(type.FullName)
            );
        }

        private YamlNode CreateArrayNode(Array array)
        {
            Type type = array.GetType();
            return CreateArrayNodeSub(array, 0, new long[type.GetArrayRank()]);
        }
        private YamlNode CreateArrayNodeSub(Array array, int i, long[] indices)
        {
            var type= array.GetType();
            var element = type.GetElementType();
            var sequence = seq();
            if ( i == 0 ) {
                sequence.Tag = TypeNameToYamlTag(type);
                AppendToAppeared(array, sequence);
            }
            if ( element.IsPrimitive || element.IsEnum || element == typeof(decimal) )
                if ( array.Rank == 1 || ArrayLength(array, i+1) < 20 )
                    sequence.Properties["Compact"] = "true";
            for ( indices[i] = 0; indices[i] < array.GetLength(i); indices[i]++ )
                if ( i == array.Rank - 1 ) {
                    var n = ObjectToNode(array.GetValue(indices), type.GetElementType());
                    sequence.Add(n);
                } else {
                    var s = CreateArrayNodeSub(array, i + 1, indices);
                    sequence.Add(s);
                }
            return sequence;
        }
        static long ArrayLength(Array array, int i)
        {
            long n = 1;
            for ( ; i < array.Rank; i++ )
                n *= array.GetLength(i);
            return n;
        }

        private YamlNode CreateBinaryArrayNode(Array array)
        {
            var type = array.GetType();
            var element = type.GetElementType();
            if ( !TypeUtils.IsPureValueType(element) )
                throw new InvalidOperationException(
                    "Can not serialize {0} as binary because it contains non-value-type(s).".DoFormat(type.FullName));
            var elementSize = Marshal.SizeOf(element);
            var binary = new byte[array.LongLength * elementSize];
            int j = 0;
            for ( int i = 0; i < array.Length; i++ ) {
                IntPtr p = Marshal.UnsafeAddrOfPinnedArrayElement(array, i);
                Marshal.Copy(p, binary, j, elementSize);
                j += elementSize;
            }
            var dimension = "";
            if ( array.Rank > 1 ) {
                for ( int i = 0; i < array.Rank; i++ ) {
                    if ( dimension != "" )
                        dimension += ", ";
                    dimension += array.GetLength(i);
                }
                dimension = "[" + dimension + "]\r\n";
            }
            var result= str(TypeNameToYamlTag(type), dimension + Base64Encode(type, binary));
            result.Properties["Don'tCareLineBreaks"] = "true";
            return result;
        }

        private static string Base64Encode(Type type, byte[] binary)
        {
            var s = System.Convert.ToBase64String(binary);
            var sb = new StringBuilder();
            for ( int i = 0; i < s.Length; i += 80 ) {
                if ( i + 80 < s.Length ) {
                    sb.AppendLine(s.Substring(i, 80));
                } else {
                    sb.AppendLine(s.Substring(i));
                }
            }
            return sb.ToString();
        }

        private YamlScalar MapKey(string key)
        {
            var node = (YamlScalar)key;
            node.Properties["expectedTag"] = YamlNode.ExpandTag("!!str");
            node.Properties["plainText"] = "true";
            return node;
        }

        private YamlMapping CreateMapping(string tag, object obj /*, bool by_content */ )
        {
            var type = obj.GetType();

            /*
            if ( type.IsClass && !by_content && type.GetConstructor(Type.EmptyTypes) == null )
                throw new ArgumentException("Type {0} has no default constructor.".DoFormat(type.FullName));
            */

            var mapping = map();
            mapping.Tag = tag;
            AppendToAppeared(obj, mapping);

            // iterate props / fields
            var accessor = ObjectMemberAccessor.FindFor(type);
            foreach ( var entry in accessor ) {
                var name = entry.Key;
                var access = entry.Value;
                if ( !access.ShouldSeriealize(obj) )
                    continue;
                if ( access.SerializeMethod == YamlSerializeMethod.Binary ) {
                    var array = CreateBinaryArrayNode((Array)access.Get(obj));
                    AppendToAppeared(access.Get(obj), array);
                    array.Properties["expectedTag"] = TypeNameToYamlTag(access.Type);
                    mapping.Add(MapKey(entry.Key), array);
                } else {
                    try {
                        var value = ObjectToNode(access.Get(obj), access.Type);
                        if( (access.SerializeMethod != YamlSerializeMethod.Content) ||
                            !(value is YamlMapping) || ((YamlMapping)value).Count>0 )
                        mapping.Add(MapKey(entry.Key), value);
                    } catch {
                    }
                }
            }
            // if the object is IDictionary or IDictionary<,>
            if ( accessor.IsDictionary && !accessor.IsReadOnly(obj) ) {
                var dictionary = DictionaryToMap(obj);
                if ( dictionary.Count > 0 )
                    mapping.Add(MapKey("IDictionary.Entries"), dictionary);
            } else {
                // if the object is ICollection<> or IList
                if ( accessor.CollectionAdd != null && !accessor.IsReadOnly(obj)) {
                    var iter = ( (IEnumerable)obj ).GetEnumerator();
                    if ( iter.MoveNext() ) { // Count > 0
                        iter.Reset();
                        mapping.Add(MapKey("ICollection.Items"), CreateSequence("!!seq", iter, accessor.ValueType));
                    }
                }
            }
            return mapping;
        }

        private YamlMapping DictionaryToMap(object obj)
        {
            var accessor = ObjectMemberAccessor.FindFor(obj.GetType());
            var iter = ( (IEnumerable)obj ).GetEnumerator();
            var dictionary = map();
            Func<object, object> key = null, value = null;
            while ( iter.MoveNext() ) {
                if ( key == null ) {
                    var keyvalue = iter.Current.GetType();
                    var keyprop = keyvalue.GetProperty("Key");
                    var valueprop = keyvalue.GetProperty("Value");
                    key = o => keyprop.GetValue(o, new object[0]);
                    value = o => valueprop.GetValue(o, new object[0]);
                }
                dictionary.Add(
                    ObjectToNode(key(iter.Current), accessor.KeyType),
                    ObjectToNode(value(iter.Current), accessor.ValueType)
                    );
            }
            return dictionary;
        }

        public YamlSequence CreateSequence(string tag, IEnumerator iter, Type expect)
        {
            var sequence = seq();
            sequence.Tag = tag;
            if ( expect != null && ( expect.IsPrimitive || expect.IsEnum || expect == typeof(decimal) ) )
                sequence.Properties["Compact"] = "true";

            while ( iter.MoveNext() )
                sequence.Add(ObjectToNode(iter.Current, expect));
            return sequence;
        }
    }
}
