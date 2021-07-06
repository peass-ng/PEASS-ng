using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Collections;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace System.Yaml.Serialization
{
    internal class ObjectActivator
    {
        Dictionary<Type, Func<object>> activators = 
            new Dictionary<Type, Func<object>>();
        public void Add<T>(Func<object> activator)
            where T: class
        {
            activators.Add(typeof(T), activator);
        }
        public T Activate<T>() where T: class
        {
            return (T)Activate(typeof(T));
        }
        public object Activate(Type type)
        {
            if ( !activators.ContainsKey(type) )
                return Activator.CreateInstance(type);                              
            return activators[type].Invoke();
        }
    }

    /// <summary>
    /// Construct YAML node tree that represents a given C# object.
    /// </summary>
    internal class YamlConstructor
    {
        /// <summary>
        /// Construct YAML node tree that represents a given C# object.
        /// </summary>
        /// <param name="node"><see cref="YamlNode"/> to be converted to C# object.</param>
        /// <param name="config"><see cref="YamlConfig"/> to customize serialization.</param>
        /// <returns></returns>
        public object NodeToObject(YamlNode node, YamlConfig config)
        {
            return NodeToObject(node, null, config);
        }
        /// <summary>
        /// Construct YAML node tree that represents a given C# object.
        /// </summary>
        /// <param name="node"><see cref="YamlNode"/> to be converted to C# object.</param>
        /// <param name="expected">Expected type for the root object.</param>
        /// <param name="config"><see cref="YamlConfig"/> to customize serialization.</param>
        /// <returns></returns>
        public object NodeToObject(YamlNode node, Type expected, YamlConfig config)
        {
            this.config = config;
            var appeared =
                new Dictionary<YamlNode, object>(TypeUtils.EqualityComparerByRef<YamlNode>.Default);
            return NodeToObjectInternal(node, expected, appeared);
        }
        YamlConfig config;

        static public YamlTagResolver TagResolver = new YamlTagResolver();

        private static Type TypeFromTag(string tag)
        {
            if ( tag.StartsWith(YamlNode.DefaultTagPrefix) ) {
                switch ( tag.Substring(YamlNode.DefaultTagPrefix.Length) ) {
                case "str":
                    return typeof(string);
                case "int":
                    return typeof(Int32);
                case "null":
                    return typeof(object);
                case "bool":
                    return typeof(bool);
                case "float":
                    return typeof(double);
                case "seq":
                case "map":
                    return null;
                default:
                    throw new NotImplementedException();
                }
            } else {
                return TypeUtils.GetType(tag.Substring(1));
            }
        }

        object NodeToObjectInternal(YamlNode node, Type expected, Dictionary<YamlNode, object> appeared)
        {
            if ( appeared.ContainsKey(node) )
                return appeared[node];

            object obj = null;
            
            // Type resolution
            Type type = expected == typeof(object) ? null : expected;
            Type fromTag = TagResolver.TypeFromTag(node.Tag);
            if ( fromTag == null )
                fromTag = TypeFromTag(node.Tag);
            if ( fromTag != null && type != fromTag && fromTag.IsClass && fromTag != typeof(string) )
                type = fromTag;
            if ( type == null )
                type = fromTag;

            // try TagResolver
            if ( type == fromTag && fromTag != null )
                if ( node is YamlScalar && TagResolver.Decode((YamlScalar)node, out obj) )
                    return obj;

            if ( node.Tag == YamlNode.DefaultTagPrefix + "null" ) {
                obj = null;
            } else
            if ( node is YamlScalar ) {
                obj = ScalarToObject((YamlScalar)node, type);
            } else
            if ( node is YamlMapping ) {
                obj = MappingToObject((YamlMapping)node, type, null, appeared);
            } else
            if ( node is YamlSequence ) {
                obj = SequenceToObject((YamlSequence)node, type, appeared);
            } else
                throw new NotImplementedException();

            if ( !appeared.ContainsKey(node) )
                if(obj != null && obj.GetType().IsClass && ( !(obj is string) || ((string)obj).Length >= 1000 ) )
                    appeared.Add(node, obj);
            
            return obj;
        }

        object ScalarToObject(YamlScalar node, Type type)
        {
            if ( type == null )
                throw new FormatException("Could not find a type '{0}'.".DoFormat(node.Tag));

            // To accommodate the !!int and !!float encoding, all "_"s in integer and floating point values
            // are simply neglected.
            if ( type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || 
                 type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) 
                 || type == typeof(float) || type == typeof(decimal) ) 
                return config.TypeConverter.ConvertFromString(node.Value.Replace("_", ""), type);

            if ( type.IsEnum || type.IsPrimitive || type == typeof(char) || type == typeof(bool) ||
                 type == typeof(string) || EasyTypeConverter.IsTypeConverterSpecified(type) )
                return config.TypeConverter.ConvertFromString(node.Value, type);

            if ( type.IsArray ) {
                // Split dimension from base64 strings
                var s = node.Value;
                var regex = new Regex(@" *\[([0-9 ,]+)\][\r\n]+((.+|[\r\n])+)");
                int[] dimension;
                byte[] binary;
                var elementSize = Marshal.SizeOf(type.GetElementType());
                if ( type.GetArrayRank() == 1 ) {
                    binary = System.Convert.FromBase64CharArray(s.ToCharArray(), 0, s.Length);
                    var arrayLength = binary.Length / elementSize;
                    dimension = new int[] { arrayLength };
                } else {
                    var m = regex.Match(s);
                    if ( !m.Success )
                        throw new FormatException("Irregal binary array");
                    // Create array from dimension
                    dimension = m.Groups[1].Value.Split(',').Select(n => Convert.ToInt32(n)).ToArray();
                    if ( type.GetArrayRank() != dimension.Length )
                        throw new FormatException("Irregal binary array");
                    // Fill values
                    s = m.Groups[2].Value;
                    binary = System.Convert.FromBase64CharArray(s.ToCharArray(), 0, s.Length);
                }
                var paramType = dimension.Select(n => typeof(int) /* n.GetType() */).ToArray();
                var array = (Array)type.GetConstructor(paramType).Invoke(dimension.Cast<object>().ToArray());
                if ( binary.Length != array.Length * elementSize )
                    throw new FormatException("Irregal binary: data size does not match to array dimension");
                int j = 0;
                for ( int i = 0; i < array.Length; i++ ) {
                    var p = Marshal.UnsafeAddrOfPinnedArrayElement(array, i);
                    Marshal.Copy(binary, j, p, elementSize);
                    j += elementSize;
                }
                return array;
            } 

            if ( node.Value == "" ) {
                return config.Activator.Activate(type);
            } else {
                return TypeDescriptor.GetConverter(type).ConvertFromString(node.Value);
            }
        }

        object SequenceToObject(YamlSequence seq, Type type, Dictionary<YamlNode, object> appeared)
        {
            if ( type == null )
                type = typeof(object[]);

            if ( type.IsArray ) {
                var lengthes= new int[type.GetArrayRank()];
                GetLengthes(seq, 0, lengthes);
                var array = (Array)type.GetConstructor(lengthes.Select(l => typeof(int) /* l.GetType() */).ToArray())
                               .Invoke(lengthes.Cast<object>().ToArray());
                appeared.Add(seq, array);
                var indices = new int[type.GetArrayRank()];
                SetArrayElements(array, seq, 0, indices, type.GetElementType(), appeared);
                return array;
            } else {
                throw new NotImplementedException();
            }
        }

        void SetArrayElements(Array array, YamlSequence seq, int i, int[] indices, Type elementType, Dictionary<YamlNode, object> appeared)
        {
            if ( i < indices.Length - 1 ) {
                for ( indices[i] = 0; indices[i] < seq.Count; indices[i]++ )
                    SetArrayElements(array, (YamlSequence)seq[indices[i]], i + 1, indices, elementType, appeared);
            } else {
                for ( indices[i] = 0; indices[i] < seq.Count; indices[i]++ )
                    array.SetValue(NodeToObjectInternal(seq[indices[i]], elementType, appeared), indices);
            }
        }

        private static void GetLengthes(YamlSequence seq, int i, int[] lengthes)
        {
            lengthes[i] = Math.Max(lengthes[i], seq.Count);
            if ( i < lengthes.Length - 1 )
                for ( int j = 0; j < seq.Count; j++ )
                    GetLengthes((YamlSequence)seq[j], i + 1, lengthes);
        }

        object MappingToObject(YamlMapping map, Type type, object obj, Dictionary<YamlNode, object> appeared)
        {
            // Naked !!map is constructed as Dictionary<object, object>.
            if ( ( ( map.ShorthandTag() == "!!map" && type == null ) || type == typeof(Dictionary<object,object>) ) && obj == null ) {
                var dict = new Dictionary<object, object>();
                appeared.Add(map, dict);
                foreach ( var entry in map ) 
                    dict.Add(NodeToObjectInternal(entry.Key, null, appeared), NodeToObjectInternal(entry.Value, null, appeared));
                return dict;
            }

            if ( obj == null ) {
                obj = config.Activator.Activate(type);
                appeared.Add(map, obj);
            } else {
                if ( appeared.ContainsKey(map) )
                    throw new InvalidOperationException("This member is not writeable: {0}".DoFormat(obj.ToString()));
            }

            var access = ObjectMemberAccessor.FindFor(type);
            foreach(var entry in map){
                if ( obj == null )
                    throw new InvalidOperationException("Object is not initialized");
                var name = (string)NodeToObjectInternal(entry.Key, typeof(string), appeared);
                switch ( name ) {
                case "ICollection.Items":
                    if ( access.CollectionAdd == null )
                        throw new FormatException("{0} is not a collection type.".DoFormat(type.FullName));
                    access.CollectionClear(obj);                                           
                    foreach(var item in (YamlSequence)entry.Value)
                        access.CollectionAdd(obj, NodeToObjectInternal(item, access.ValueType, appeared));
                    break;
                case "IDictionary.Entries":
                    if ( !access.IsDictionary )
                        throw new FormatException("{0} is not a dictionary type.".DoFormat(type.FullName));
                    var dict = obj as IDictionary;
                    dict.Clear();
                    foreach ( var child in (YamlMapping)entry.Value )
                        dict.Add(NodeToObjectInternal(child.Key, access.KeyType, appeared), NodeToObjectInternal(child.Value, access.ValueType, appeared));
                    break;
                default:
                        if (!access.ContainsKey(name))
                            // ignoring non existing properties
                            //throw new FormatException("{0} does not have a member {1}.".DoFormat(type.FullName, name));
                            continue;
                    switch ( access[name].SerializeMethod ) {
                    case YamlSerializeMethod.Assign:
                        access[obj, name] = NodeToObjectInternal(entry.Value, access[name].Type, appeared);
                        break;
                    case YamlSerializeMethod.Content:
                        MappingToObject((YamlMapping)entry.Value, access[name].Type, access[obj, name], appeared);
                        break;
                    case YamlSerializeMethod.Binary:
                        access[obj, name] = ScalarToObject((YamlScalar)entry.Value, access[name].Type);
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Member {0} of {1} is not serializable.".DoFormat(name, type.FullName));
                    }
                    break;
                }
            }
            return obj;
        }

    }
}
