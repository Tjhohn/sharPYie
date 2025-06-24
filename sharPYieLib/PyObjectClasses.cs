using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace sharPYieLib
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

        public virtual PyBaseObject Add(PyBaseObject other)
        => throw new Exception($"TypeError: unsupported operand type(s) for +: '{TypeName}' and '{other.TypeName}'");

        public virtual PyBaseObject Subtract(PyBaseObject other)
            => throw new Exception($"TypeError: unsupported operand type(s) for -: '{TypeName}' and '{other.TypeName}'");

        public virtual PyBaseObject Multiply(PyBaseObject other)
            => throw new Exception($"TypeError: unsupported operand type(s) for *: '{TypeName}' and '{other.TypeName}'");

        public virtual PyBaseObject Divide(PyBaseObject other)
            => throw new Exception($"TypeError: unsupported operand type(s) for /: '{TypeName}' and '{other.TypeName}'");

        public virtual PyBaseObject Eq(PyBaseObject other)
            => new PyBool(ReferenceEquals(this, other));

        public virtual PyBaseObject LessThan(PyBaseObject other)
            => throw new Exception($"TypeError: '<' not supported between instances of '{TypeName}' and '{other.TypeName}'");

        public virtual PyBaseObject LessThanOrEqual(PyBaseObject other)
            => throw new Exception($"TypeError: '<=' not supported between instances of '{TypeName}' and '{other.TypeName}'");

        public virtual PyBaseObject GreaterThan(PyBaseObject other)
            => throw new Exception($"TypeError: '>' not supported between instances of '{TypeName}' and '{other.TypeName}'");

        public virtual PyBaseObject GreaterThanOrEqual(PyBaseObject other)
            => throw new Exception($"TypeError: '>=' not supported between instances of '{TypeName}' and '{other.TypeName}'");

        // Optional: unary minus
        public virtual PyBaseObject Negate()
            => throw new Exception($"TypeError: bad operand type for unary -: '{TypeName}'");
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

        public override PyBaseObject Subtract(PyBaseObject other)
            => other is PyInt i ? new PyInt(Value - i.Value)
               : other is PyFloat f ? new PyFloat(Value - f.Value)
               : base.Subtract(other);

        public override PyBaseObject Multiply(PyBaseObject other)
            => other is PyInt i ? new PyInt(Value * i.Value)
               : other is PyFloat f ? new PyFloat(Value * f.Value)
               : base.Multiply(other);

        public override PyBaseObject Divide(PyBaseObject other)
        {
            if (other is PyInt intOther)
            {
                if (intOther.Value == 0)
                    throw new DivideByZeroException();
                return new PyFloat((double)Value / intOther.Value);
            }
            else if (other is PyFloat floatOther)
            {
                if (floatOther.Value == 0.0)
                    throw new DivideByZeroException();
                return new PyFloat(Value / floatOther.Value);
            }

            return base.Divide(other);
        }

        public override PyBaseObject Eq(PyBaseObject other)
            => other is PyInt i ? new PyBool(Value == i.Value)
             : other is PyFloat f ? new PyBool(Value == f.Value)
             : new PyBool(false);

        public override PyBaseObject LessThan(PyBaseObject other)
            => other is PyInt i ? new PyBool(Value < i.Value)
             : other is PyFloat f ? new PyBool(Value < f.Value)
             : base.LessThan(other);

        public override PyBaseObject Negate() => new PyInt(-Value);

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
            return other switch
            {
                PyFloat f => new PyFloat(Value + f.Value),
                PyInt i => new PyFloat(Value + i.Value),
                _ => base.Add(other)
            };
        }

        public override PyBaseObject Subtract(PyBaseObject other)
        {
            return other switch
            {
                PyFloat f => new PyFloat(Value - f.Value),
                PyInt i => new PyFloat(Value - i.Value),
                _ => base.Subtract(other)
            };
        }

        public override PyBaseObject Multiply(PyBaseObject other)
        {
            return other switch
            {
                PyFloat f => new PyFloat(Value * f.Value),
                PyInt i => new PyFloat(Value * i.Value),
                _ => base.Multiply(other)
            };
        }

        public override PyBaseObject Divide(PyBaseObject other)
        {
            return other switch
            {
                PyFloat f when f.Value == 0.0 => throw new DivideByZeroException(),
                PyInt i when i.Value == 0 => throw new DivideByZeroException(),
                PyFloat f => new PyFloat(Value / f.Value),
                PyInt i => new PyFloat(Value / i.Value),
                _ => base.Divide(other)
            };
        }

        public override PyBaseObject Eq(PyBaseObject other)
        {
            return other switch
            {
                PyFloat f => new PyBool(Value == f.Value),
                PyInt i => new PyBool(Value == i.Value),
                _ => new PyBool(false)
            };
        }

        public override PyBaseObject LessThan(PyBaseObject other)
        {
            return other switch
            {
                PyFloat f => new PyBool(Value < f.Value),
                PyInt i => new PyBool(Value < i.Value),
                _ => base.LessThan(other)
            };
        }

        public override PyBaseObject GreaterThan(PyBaseObject other)
        {
            return other switch
            {
                PyFloat f => new PyBool(Value > f.Value),
                PyInt i => new PyBool(Value > i.Value),
                _ => base.GreaterThan(other)
            };
        }

        public override PyBaseObject LessThanOrEqual(PyBaseObject other)
        {
            return other switch
            {
                PyFloat f => new PyBool(Value <= f.Value),
                PyInt i => new PyBool(Value <= i.Value),
                _ => base.LessThanOrEqual(other)
            };
        }

        public override PyBaseObject GreaterThanOrEqual(PyBaseObject other)
        {
            return other switch
            {
                PyFloat f => new PyBool(Value >= f.Value),
                PyInt i => new PyBool(Value >= i.Value),
                _ => base.GreaterThanOrEqual(other)
            };
        }

        public override PyBaseObject Negate() => new PyFloat(-Value);

        public override string Str() => Value.ToString("G");
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

    public class PyList : PyBaseObject
    {
        public List<PyBaseObject> Items { get; }

        public PyList(List<PyBaseObject> items) => Items = items;

        public override PyBaseObject GetAttr(string name)
        {
            if (name == "append")
            {
                return new PyNativeFunction(args =>
                {
                    if (args.Count != 1) throw new Exception("append takes 1 arg");
                    Items.Add(args[0]);
                    return PyNone.Instance;
                });
            }

            if (name == "pop")
            {
                return new PyNativeFunction(args =>
                {
                    if (Items.Count == 0)
                        throw new Exception("pop from empty list");
                    var last = Items[^1];
                    Items.RemoveAt(Items.Count - 1);
                    return last;
                });
            }

            return base.GetAttr(name);
        }

        public override string TypeName => "list";
        public override string Repr() => "[" + string.Join(", ", Items.Select(i => i.Repr())) + "]";
        public override PyBaseObject Len() => new PyInt(Items.Count);
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
    // class for python class builtins
    public class PyNativeFunction : PyBaseObject
    {
        private readonly Func<List<PyBaseObject>, PyBaseObject> impl;

        public PyNativeFunction(Func<List<PyBaseObject>, PyBaseObject> impl) => this.impl = impl;

        public override string TypeName => "native_function";
        public override PyBaseObject Call(List<PyBaseObject> args) => impl(args);
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

        public static object Unwrap(PyBaseObject obj) => obj switch
        {
            PyInt i => i.Value,
            PyFloat f => f.Value,
            PyBool b => b.Value,
            PyStr s => s.Value,
            PyNone => null!,
            PyList l => l.Items.Select(Unwrap).ToList(),
            PyDict d => d.Items().ToDictionary(kvp => Unwrap(kvp.Key), kvp => Unwrap(kvp.Value)),
            _ => throw new Exception($"Unsupported type for unwrapping: {obj.TypeName}")
        };
    }


}
