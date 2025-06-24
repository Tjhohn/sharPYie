using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace sharPYieLib
{
    internal class PyObjectClasses
    {

        public abstract class PyBaseObject
        {
            protected Dictionary<string, PyBaseObject> attributes = new();

            public virtual string TypeName => "object";

            public virtual PyBaseObject GetAttr(string name)
            {
                if (attributes.TryGetValue(name, out var val)) return val;
                throw new Exception($"AttributeError: '{TypeName}' object has no attribute '{name}'");
            }

            public virtual void SetAttr(string name, PyBaseObject value)
            {
                attributes[name] = value;
            }

            public virtual PyBaseObject Call(List<PyBaseObject> args) =>
                throw new Exception($"{TypeName} object is not callable");

            public virtual PyBaseObject Add(PyBaseObject other) =>
                throw new Exception($"Unsupported operand types for +: '{TypeName}' and '{other.TypeName}'");

            public virtual PyBaseObject Eq(PyBaseObject other) =>
                new PyBool(ReferenceEquals(this, other));

            public virtual PyBaseObject Len() =>
                throw new Exception($"Object of type '{TypeName}' has no len()");

            public virtual string Repr() => $"<{TypeName} object>";
            public virtual string Str() => Repr();
        }

        public class PyObject : PyBaseObject
        {
            public override string TypeName => "object";

            public PyObject() { }

            // This object is generic, so only attribute behavior is available.
            // You can extend it to have a class pointer in future for full Python class support.
        }

        public class PyInt : PyBaseObject
        {
            public int Value { get; }
            public PyInt(int val) => Value = val;

            public override string TypeName => "int";

            public override PyBaseObject Add(PyBaseObject other)
            {
                if (other is PyInt i)
                    return new PyInt(Value + i.Value);
                if (other is PyFloat f)
                    return new PyFloat(Value + f.Value);
                return base.Add(other);
            }

            public override PyBaseObject Eq(PyBaseObject other)
                => other is PyInt i && i.Value == Value ? new PyBool(true) : new PyBool(false);

            public override string Repr() => Value.ToString();
            public override string Str() => Value.ToString();
        }

        public class PyFloat : PyBaseObject
        {
            public double Value { get; }
            public PyFloat(double val) => Value = val;

            public override string TypeName => "float";

            public override PyBaseObject Add(PyBaseObject other)
            {
                if (other is PyFloat f)
                    return new PyFloat(Value + f.Value);
                if (other is PyInt i)
                    return new PyFloat(Value + i.Value);
                return base.Add(other);
            }

            public override string Repr() => Value.ToString("G");
        }

        public class PyStr : PyBaseObject
        {
            public string Value { get; }
            public PyStr(string val) => Value = val;

            public override string TypeName => "str";

            public override PyBaseObject Add(PyBaseObject other)
            {
                if (other is PyStr s)
                    return new PyStr(Value + s.Value);
                return base.Add(other);
            }

            public override PyBaseObject Len() => new PyInt(Value.Length);
            public override string Repr() => $"'{Value}'";
            public override string Str() => Value;
        }

        public class PyBool : PyBaseObject
        {
            public bool Value { get; }
            public PyBool(bool val) => Value = val;

            public override string TypeName => "bool";
            public override string Repr() => Value ? "True" : "False";
        }

        public sealed class PyNone : PyBaseObject
        {
            public static readonly PyNone Instance = new PyNone();
            private PyNone() { }

            public override string TypeName => "NoneType";
            public override string Repr() => "None";
        }

        public class PyDict : PyBaseObject
        {
            private readonly Dictionary<PyBaseObject, PyBaseObject> dict = new();

            public override string TypeName => "dict";

            public PyDict() { }

            public void SetItem(PyBaseObject key, PyBaseObject value)
            {
                dict[key] = value;
            }

            public PyBaseObject GetItem(PyBaseObject key)
            {
                if (!dict.TryGetValue(key, out var val))
                    throw new Exception($"KeyError: {key.Repr()}");
                return val;
            }

            public override PyBaseObject Len() => new PyInt(dict.Count);

            public override string Repr()
            {
                var entries = dict.Select(kvp => $"{kvp.Key.Repr()}: {kvp.Value.Repr()}");
                return "{" + string.Join(", ", entries) + "}";
            }

            public override string Str() => Repr();

            public IEnumerable<KeyValuePair<PyBaseObject, PyBaseObject>> Items() => dict;
        }
        /// Python user defined class junk
        public class PyBoundMethod : PyBaseObject
        {
            private readonly PyInstance self;
            private readonly Func<PyInstance, List<PyBaseObject>, PyBaseObject> method;

            public PyBoundMethod(PyInstance self, Func<PyInstance, List<PyBaseObject>, PyBaseObject> method)
            {
                this.self = self;
                this.method = method;
            }

            public override string TypeName => "bound_method";

            public override PyBaseObject Call(List<PyBaseObject> args)
            {
                return method(self, args);
            }

            public override string Repr() => $"<bound method of {self.TypeName}>";
        }

        public class PyClass : PyBaseObject
        {
            public string Name { get; }
            public Dictionary<string, Func<PyInstance, List<PyBaseObject>, PyBaseObject>> Methods { get; }

            public PyClass(string name)
            {
                Name = name;
                Methods = new();
            }

            public override PyBaseObject Call(List<PyBaseObject> args)
            {
                var instance = new PyInstance(this);
                if (Methods.TryGetValue("__init__", out var init))
                    init(instance, args);
                return instance;
            }

            public override string TypeName => "type";
        }

        public class PyInstance : PyBaseObject
        {
            private readonly PyClass klass;
            public PyInstance(PyClass klass) => this.klass = klass;

            public override PyBaseObject GetAttr(string name)
            {
                if (attributes.TryGetValue(name, out var val)) return val;
                if (klass.Methods.TryGetValue(name, out var method))
                    return new PyBoundMethod(this, method);
                return base.GetAttr(name);
            }

            public override string TypeName => klass.Name;
        }


        public static class PyConverter
        {
            public static PyBaseObject Wrap(object obj) => obj switch
            {
                PyBaseObject py => py,
                int i => new PyInt(i),
                double d => new PyFloat(d),
                string s => new PyStr(s),
                bool b => new PyBool(b),
                Dictionary<object, object> d => WrapDict(d),
                null => PyNone.Instance,
                _ => throw new Exception($"Unsupported object type: {obj.GetType().Name}")
            };

            private static PyDict WrapDict(Dictionary<object, object> dict)
            {
                var pyDict = new PyDict();
                foreach (var (key, val) in dict)
                    pyDict.SetItem(Wrap(key), Wrap(val));
                return pyDict;
            }
        }
    }
}
