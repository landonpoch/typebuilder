using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TypeBuilder
{
    public class Node
    {
        public NodeType Type { get; set; }
        public object Value { get; set; }
    }

    public class NamedNode : Node
    {
        public NamedNode(string name) { Name = name; }
        public string Name { get; private set; }
    }

    public enum NodeType
    {
        Array,
        Binary,
        Double,
        Integer,
        Object,
        String,
        Null,
        Boolean
    }

    public class Converter
    {
        private IDictionary<Type, PropertyInfo[]> _getCache = new Dictionary<Type, PropertyInfo[]>();
        private IDictionary<Type, Type[]> _genericCache = new Dictionary<Type, Type[]>();
        private IDictionary<Type, NodeType> _mapCache = new Dictionary<Type, NodeType>();

        public Node Serialize<T>(T obj)
        {
            return Serialize(typeof(T), obj);
        }

        private Node Serialize(Type type, object obj)
        {
            var mappedType = Map(type);

            if (obj == null)
                return new Node { Type = NodeType.Null, Value = null };

            if (mappedType == NodeType.Double
                || mappedType == NodeType.Integer
                || mappedType == NodeType.String
                || mappedType == NodeType.Boolean)
                return new Node { Type = mappedType, Value = obj };

            if (mappedType == NodeType.Array)
                return new Node { Type = mappedType, Value = ParseArray(type, (IList)obj) };

            return new Node { Type = mappedType, Value = ParseObject(type, obj) };
        }

        private object ParseArray(Type type, IList values)
        {
            var nodes = new List<Node>();
            var cached = _genericCache.ContainsKey(type);
            var genericTypes = cached
                ? _genericCache[type]
                : type.GetGenericArguments();
            if (!cached) _genericCache[type] = genericTypes;

            foreach (var value in values)
                nodes.Add(Serialize(genericTypes[0], value));

            return nodes;
        }

        private object ParseObject(Type type, object obj)
        {
            var cached = _getCache.ContainsKey(type);
            var properties =  cached
                ? _getCache[type] 
                : type.GetProperties();
            if (!cached) _getCache[type] = properties;

            var namedNodes = new List<NamedNode>();
            foreach (var property in properties)
            {
                NamedNode namedNode = new NamedNode(property.Name);
                namedNode.Type = Map(property.PropertyType);
                if (namedNode.Type == NodeType.Double
                    || namedNode.Type == NodeType.Integer
                    || namedNode.Type == NodeType.String
                    || namedNode.Type == NodeType.Boolean)
                {
                    namedNode.Value = property.GetValue(obj);
                }
                else if (namedNode.Type == NodeType.Array)
                {
                    namedNode.Value = ParseArray(property.PropertyType, (IList)property.GetValue(obj));
                }
                else
                {
                    namedNode.Value = ParseObject(property.PropertyType, property.GetValue(obj));
                }
                namedNodes.Add(namedNode);
            }
            return namedNodes;
        }

        public T Deserialize<T>(Node node)
        {
            return (T)Deserialize(typeof(T), node);
        }

        private object Deserialize(Type type, Node node)
        {
            if (node.Type == NodeType.Null)
                return null;

            if (type == typeof(double)
                || type == typeof(decimal) // TODO: Add other types
                || type == typeof(int)
                || type == typeof(string)
                || type == typeof(bool))
                return node.Value;
            if (IsCollection(type))
                return ParseArrayNode(type, (List<Node>)node.Value);

            return ParseObjectNode(type, (List<NamedNode>)node.Value);
        }

        public object ParseArrayNode(Type type, List<Node> children)
        {
            var array = (IList)Activator.CreateInstance(type);

            var cached = _genericCache.ContainsKey(type);
            var genericTypes = cached
                ? _genericCache[type]
                : type.GetGenericArguments();
            if (!cached) _genericCache[type] = genericTypes;

            foreach (var child in children)
                array.Add(Deserialize(genericTypes[0], child));
            return array;
        }

        public object ParseObjectNode(Type type, List<NamedNode> children)
        {
            var instance = Activator.CreateInstance(type);

            var cached = _getCache.ContainsKey(type);
            var properties = cached
                ? _getCache[type]
                : type.GetProperties();
            if (!cached) _getCache[type] = properties;

            foreach (var property in properties)
            {
                var child = children.FirstOrDefault(c => c.Name == property.Name);
                if (child != null)
                {
                    if (property.PropertyType == typeof(double)
                        || property.PropertyType == typeof(decimal) // TODO: Add other types
                        || property.PropertyType == typeof(int)
                        || property.PropertyType == typeof(string)
                        || property.PropertyType == typeof(bool))
                    {
                        property.SetValue(instance, child.Value);
                    }
                    else if (IsCollection(property.PropertyType))
                    {
                        property.SetValue(instance, ParseArrayNode(property.PropertyType, (List<Node>)child.Value));
                    }
                    else
                    {
                        property.SetValue(instance, ParseObjectNode(property.PropertyType, (List<NamedNode>)child.Value));
                    }
                }    
            }

            return instance;
        }

        private NodeType Map(Type type)
        {
            var cached = _mapCache.ContainsKey(type);
            NodeType nodeType = cached
                ? _mapCache[type]
                : UnCachedMap(type);
            if (!cached) _mapCache[type] = nodeType;

            return nodeType;
        }

        private NodeType UnCachedMap(Type type)
        {
            if (type == typeof(double)
                || type == typeof(decimal)) // TODO: Add other types
                return NodeType.Double;
            if (type == typeof(int))
                return NodeType.Integer;
            if (type == typeof(string))
                return NodeType.String;
            if (type == typeof(bool))
                return NodeType.Boolean;
            if (IsCollection(type))
                return NodeType.Array;
            return NodeType.Object;
        }

        private bool IsCollection(Type type)
        {
            foreach (var @interface in type.GetInterfaces())
            {
                if (@interface.GenericTypeArguments.Any()
                    && @interface.GetGenericTypeDefinition() == typeof(IList<>))
                    return true;
            }
            return false;
        }
    }
}
