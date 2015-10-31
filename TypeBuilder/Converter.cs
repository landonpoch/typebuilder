using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TypeBuilder
{
    public class Node
    {
        public NodeType Type { get; set; }
        public object Value { get; set; }
    }

    public class NamedNode : Node
    {
        public NamedNode(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }

    public enum NodeType
    {
        Array,
        Binary,
        Double,
        Integer,
        Object,
        String
    }

    public class Converter
    {
        public Node Serialize<T>(T obj)
        {
            var type = obj.GetType();
            return Serialize(type, obj);
        }

        private Node Serialize(Type type, object obj)
        {
            if (type == typeof(double))
                return new Node { Type = NodeType.Double, Value = obj };
            if (type == typeof(int))
                return new Node { Type = NodeType.Integer, Value = obj };
            if (type == typeof(string))
                return new Node { Type = NodeType.String, Value = obj };
            if (IsCollection(type))
                return new Node { Type = NodeType.Array, Value = ParseArray(type, obj) };

            return new Node { Type = NodeType.Object, Value = ParseObject(type, obj) };
        }

        private object ParseArray(Type type, object value)
        {
            var nodes = new List<Node>();
            var arrayType = type.GetGenericArguments()[0];

            dynamic items = value;
            foreach (var item in items)
                nodes.Add(Serialize(arrayType, item));

            return nodes;
        }

        private object ParseObject(Type type, object obj)
        {
            var properties = type.GetProperties();

            var namedNodes = new List<NamedNode>();
            foreach (var property in properties)
            {
                NamedNode namedNode = new NamedNode(property.Name);
                if (property.PropertyType == typeof(double))
                {
                    namedNode.Type = NodeType.Double;
                    namedNode.Value = property.GetValue(obj);
                }
                else if (property.PropertyType == typeof(int))
                {
                    namedNode.Type = NodeType.Integer;
                    namedNode.Value = property.GetValue(obj);
                }
                else if (property.PropertyType == typeof(string))
                {
                    namedNode.Type = NodeType.String;
                    namedNode.Value = property.GetValue(obj);
                }
                else if (IsCollection(property.PropertyType))
                {
                    namedNode.Type = NodeType.Array;
                    namedNode.Value = ParseArray(property.PropertyType, property.GetValue(obj));
                }
                else
                {
                    namedNode.Type = NodeType.Object;
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

        public object Deserialize(Type type, Node node)
        {
            if (type == typeof(double)
                || type == typeof(int)
                || type == typeof(string))
                return node.Value;
            if (IsCollection(type))
                return ParseArrayNode(type, (List<Node>)node.Value);

            return ParseObjectNode(type, (List<NamedNode>)node.Value);
        }

        public object ParseArrayNode(Type type, List<Node> children)
        {
            var array = (IList)Activator.CreateInstance(type);
            var arrayType = type.GetGenericArguments()[0];
            foreach (var child in children)
                array.Add(Deserialize(arrayType, child));
            return array;
        }

        public object ParseObjectNode(Type type, List<NamedNode> children)
        {
            var instance = Activator.CreateInstance(type);
            var properties = type.GetProperties();
            foreach (var property in properties)
            {
                var child = children.FirstOrDefault(c => c.Name == property.Name);
                if (child != null)
                {
                    if (property.PropertyType == typeof(double)
                        || property.PropertyType == typeof(int)
                        || property.PropertyType == typeof(string))
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
            if (IsCollection(type))
                return NodeType.Array;
            if (type == typeof(double))
                return NodeType.Double;
            if (type == typeof(int))
                return NodeType.Integer;
            if (type == typeof(string))
                return NodeType.String;
            return NodeType.Object;
        }

        private bool IsCollection(Type type)
        {
            foreach (var @interface in type.GetInterfaces())
            {
                if (@interface.GetGenericTypeDefinition() == typeof(IList<>))
                    return true;
            }
            return false;
        }
    }
}
