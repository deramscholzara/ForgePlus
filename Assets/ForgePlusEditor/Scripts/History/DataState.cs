#if !NO_EDITING
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;

namespace ForgePlus.History
{
    // Deep copies, comparisons and in-place restores of Aleph One's level data, compiled once for each type. Restoring
    // copies into existing objects where it can, so whatever refers to them (such as the level's entities) sees the result.
    [NoAutoStaticsCleanup]
    public static class DataState
    {
        private sealed class Operations
        {
            public Func<object, object> Clone;
            public Func<object, object, bool> AreEqual;

            public Func<object, object, object> CopyInto;
        }

        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        // Kept between Play sessions (the compiled code doesn't change), and only used on the main thread
        private static readonly Dictionary<Type, Operations> operationsByType = new Dictionary<Type, Operations>();
        private static readonly Dictionary<Type, bool> plainValueTypes = new Dictionary<Type, bool>();

        public static object Clone(object value)
        {
            if (value == null)
            {
                return null;
            }

            return OperationsFor(value.GetType()).Clone(value);
        }

        public static bool AreEqual(object a, object b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a == null || b == null || a.GetType() != b.GetType())
            {
                return false;
            }

            return OperationsFor(a.GetType()).AreEqual(a, b);
        }

        // The target with the source's values, or a copy of the source if the target can't take them (null, or another
        // type)
        public static object CopyInto(object target, object source)
        {
            if (source == null)
            {
                return null;
            }

            if (target == null || target.GetType() != source.GetType() || ReferenceEquals(target, source))
            {
                return Clone(source);
            }

            return OperationsFor(source.GetType()).CopyInto(target, source);
        }

        // Numbers, strings, enums, and structs of only those, which are copied by assigning them
        public static bool IsPlainValue(Type type)
        {
            if (plainValueTypes.TryGetValue(type, out var isPlain))
            {
                return isPlain;
            }

            // Assumed until worked out (a struct can't contain itself)
            plainValueTypes[type] = false;

            isPlain = type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
            if (!isPlain && type.IsValueType)
            {
                isPlain = true;
                foreach (var field in AllInstanceFields(type))
                {
                    if (!IsPlainValue(field.FieldType))
                    {
                        isPlain = false;
                        break;
                    }
                }
            }

            plainValueTypes[type] = isPlain;

            return isPlain;
        }

        // The names of the fields that differ (through reflection, so only for the few objects that changed), or all of
        // them for an object added or removed (null)
        public static HashSet<string> ChangedFields(object a, object b)
        {
            var changedFields = new HashSet<string>();
            var type = (a ?? b)?.GetType();
            if (type == null)
            {
                return changedFields;
            }

            foreach (var field in AllInstanceFields(type))
            {
                if (a == null || b == null || !AreEqual(field.GetValue(a), field.GetValue(b)))
                {
                    changedFields.Add(field.Name);
                }
            }

            return changedFields;
        }

        private static Operations OperationsFor(Type type)
        {
            if (!operationsByType.TryGetValue(type, out var operations))
            {
                operations = BuildOperations(type);
                operationsByType[type] = operations;
            }

            return operations;
        }

        // Collections check it for every element
        private static class PlainValue<T>
        {
            public static readonly bool Is = IsPlainValue(typeof(T));
        }

        private static Operations BuildOperations(Type type)
        {
            if (IsPlainValue(type))
            {
                return new Operations
                {
                    Clone = value => value,
                    AreEqual = (a, b) => a.Equals(b),
                    CopyInto = (target, source) => source,
                };
            }

            if (type.IsArray)
            {
                return CollectionOperations(nameof(CloneArray), nameof(ArraysAreEqual), nameof(CopyIntoArray), type.GetElementType());
            }

            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                var arguments = type.GetGenericArguments();

                if (definition == typeof(List<>))
                {
                    return CollectionOperations(nameof(CloneList), nameof(ListsAreEqual), nameof(CopyIntoList), arguments);
                }

                if (definition == typeof(HashSet<>))
                {
                    return CollectionOperations(nameof(CloneSet), nameof(SetsAreEqual), nameof(CopyIntoSet), arguments);
                }

                if (definition == typeof(Dictionary<,>))
                {
                    return CollectionOperations(nameof(CloneDictionary), nameof(DictionariesAreEqual), nameof(CopyIntoDictionary), arguments);
                }
            }

            return ObjectOperations(type);
        }

        private static Operations CollectionOperations(string clone, string areEqual, string copyInto, params Type[] typeArguments)
        {
            return new Operations
            {
                Clone = CreateDelegate<Func<object, object>>(clone, typeArguments),
                AreEqual = CreateDelegate<Func<object, object, bool>>(areEqual, typeArguments),
                CopyInto = CreateDelegate<Func<object, object, object>>(copyInto, typeArguments),
            };
        }

        private static TDelegate CreateDelegate<TDelegate>(string methodName, Type[] typeArguments) where TDelegate : Delegate
        {
            var method = typeof(DataState).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(typeArguments);

            return (TDelegate) method.CreateDelegate(typeof(TDelegate));
        }

        #region Classes and structs

        // Plain fields are assigned and compared directly, and others go through DataState (by their actual type)
        private static Operations ObjectOperations(Type type)
        {
            var fields = AllInstanceFields(type);

            // Aleph One's data is classes with parameterless constructors
            if (type.IsValueType || type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null || fields.Exists(field => field.IsInitOnly))
            {
                throw new NotSupportedException($"{type} isn't plain data, so it can't be part of an undoable state.");
            }

            var sourceObject = Expression.Parameter(typeof(object), "sourceObject");
            var targetObject = Expression.Parameter(typeof(object), "targetObject");
            var source = Expression.Variable(type, "source");
            var target = Expression.Variable(type, "target");

            var castSource = Expression.Assign(source, Expression.Convert(sourceObject, type));
            var castTarget = Expression.Assign(target, Expression.Convert(targetObject, type));

            var cloneBody = new List<Expression>
            {
                castSource,
                Expression.Assign(target, Expression.New(type)),
            };

            foreach (var field in fields)
            {
                cloneBody.Add(Expression.Assign(Expression.Field(target, field), CopiedValue(Expression.Field(source, field), field.FieldType)));
            }

            cloneBody.Add(Expression.Convert(target, typeof(object)));

            var copyIntoBody = new List<Expression> { castSource, castTarget };

            foreach (var field in fields)
            {
                copyIntoBody.Add(Expression.Assign(Expression.Field(target, field), CopiedIntoValue(Expression.Field(target, field), Expression.Field(source, field), field.FieldType)));
            }

            copyIntoBody.Add(Expression.Convert(target, typeof(object)));

            Expression equality = Expression.Constant(true);
            for (var i = fields.Count - 1; i >= 0; i--)
            {
                var field = fields[i];
                equality = Expression.AndAlso(FieldsAreEqual(Expression.Field(source, field), Expression.Field(target, field), field.FieldType), equality);
            }

            var areEqualBody = Expression.Block(typeof(bool), new[] { source, target }, castSource, castTarget, equality);

            return new Operations
            {
                Clone = Expression.Lambda<Func<object, object>>(Expression.Block(typeof(object), new[] { source, target }, cloneBody), sourceObject).Compile(),
                AreEqual = Expression.Lambda<Func<object, object, bool>>(areEqualBody, sourceObject, targetObject).Compile(),
                CopyInto = Expression.Lambda<Func<object, object, object>>(Expression.Block(typeof(object), new[] { source, target }, copyIntoBody), targetObject, sourceObject).Compile(),
            };
        }

        private static Expression CopiedValue(Expression value, Type type)
        {
            if (IsPlainValue(type))
            {
                return value;
            }

            return Expression.Convert(Expression.Call(typeof(DataState), nameof(Clone), null, Expression.Convert(value, typeof(object))), type);
        }

        private static Expression CopiedIntoValue(Expression targetValue, Expression sourceValue, Type type)
        {
            if (IsPlainValue(type))
            {
                return sourceValue;
            }

            return Expression.Convert(Expression.Call(typeof(DataState), nameof(CopyInto), null, targetValue, sourceValue), type);
        }

        private static Expression FieldsAreEqual(Expression a, Expression b, Type type)
        {
            if (type.IsPrimitive && type != typeof(float) && type != typeof(double) || type.IsEnum)
            {
                return Expression.Equal(a, b);
            }

            if (type == typeof(string))
            {
                return Expression.Call(typeof(string), nameof(string.Equals), null, a, b);
            }

            if (IsPlainValue(type))
            {
                // Also floats, so NaN equals itself
                return Expression.Call(typeof(DataState), nameof(PlainValuesAreEqual), new[] { type }, a, b);
            }

            return Expression.Call(typeof(DataState), nameof(AreEqual), null, Expression.Convert(a, typeof(object)), Expression.Convert(b, typeof(object)));
        }

        private static List<FieldInfo> AllInstanceFields(Type type)
        {
            var fields = new List<FieldInfo>();

            for (var current = type; current != null && current != typeof(object) && current != typeof(ValueType); current = current.BaseType)
            {
                fields.AddRange(current.GetFields(InstanceFields));
            }

            return fields;
        }

        private static bool PlainValuesAreEqual<T>(T a, T b)
        {
            return EqualityComparer<T>.Default.Equals(a, b);
        }

        #endregion Classes and structs

        #region Collections

        private static object CloneArray<T>(object sourceObject)
        {
            var source = (T[]) sourceObject;

            if (PlainValue<T>.Is)
            {
                return source.Clone();
            }

            var copy = new T[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                copy[i] = (T) Clone(source[i]);
            }

            return copy;
        }

        private static bool ArraysAreEqual<T>(object aObject, object bObject)
        {
            var a = (T[]) aObject;
            var b = (T[]) bObject;

            return a.Length == b.Length && ElementsAreEqual(a, b, a.Length);
        }

        private static object CopyIntoArray<T>(object targetObject, object sourceObject)
        {
            var target = (T[]) targetObject;
            var source = (T[]) sourceObject;

            if (target.Length != source.Length)
            {
                return CloneArray<T>(source);
            }

            CopyElementsInto(target, source, source.Length);

            return target;
        }

        private static object CloneList<T>(object sourceObject)
        {
            var source = (List<T>) sourceObject;
            var copy = new List<T>(source.Count);

            if (PlainValue<T>.Is)
            {
                copy.AddRange(source);
                return copy;
            }

            foreach (var element in source)
            {
                copy.Add((T) Clone(element));
            }

            return copy;
        }

        private static bool ListsAreEqual<T>(object aObject, object bObject)
        {
            var a = (List<T>) aObject;
            var b = (List<T>) bObject;

            return a.Count == b.Count && ElementsAreEqual(a, b, a.Count);
        }

        // Keeping the list, and its elements where there are as many
        private static object CopyIntoList<T>(object targetObject, object sourceObject)
        {
            var target = (List<T>) targetObject;
            var source = (List<T>) sourceObject;

            if (target.Count > source.Count)
            {
                target.RemoveRange(source.Count, target.Count - source.Count);
            }

            var sharedCount = target.Count;
            CopyElementsInto(target, source, sharedCount);

            for (var i = sharedCount; i < source.Count; i++)
            {
                target.Add((T) Clone(source[i]));
            }

            return target;
        }

        private static object CloneSet<T>(object sourceObject)
        {
            var source = (HashSet<T>) sourceObject;
            var copy = new HashSet<T>(source.Comparer);

            foreach (var element in source)
            {
                copy.Add((T) Clone(element));
            }

            return copy;
        }

        private static bool SetsAreEqual<T>(object aObject, object bObject)
        {
            var a = (HashSet<T>) aObject;
            var b = (HashSet<T>) bObject;

            return a.Count == b.Count && a.SetEquals(b);
        }

        private static object CopyIntoSet<T>(object targetObject, object sourceObject)
        {
            var target = (HashSet<T>) targetObject;

            target.Clear();
            foreach (var element in (HashSet<T>) sourceObject)
            {
                target.Add((T) Clone(element));
            }

            return target;
        }

        private static object CloneDictionary<TKey, TValue>(object sourceObject)
        {
            var source = (Dictionary<TKey, TValue>) sourceObject;
            var copy = new Dictionary<TKey, TValue>(source.Count, source.Comparer);

            foreach (var pair in source)
            {
                copy.Add((TKey) Clone(pair.Key), (TValue) Clone(pair.Value));
            }

            return copy;
        }

        private static bool DictionariesAreEqual<TKey, TValue>(object aObject, object bObject)
        {
            var a = (Dictionary<TKey, TValue>) aObject;
            var b = (Dictionary<TKey, TValue>) bObject;

            if (a.Count != b.Count)
            {
                return false;
            }

            foreach (var pair in a)
            {
                if (!b.TryGetValue(pair.Key, out var bValue) || !AreEqual(pair.Value, bValue))
                {
                    return false;
                }
            }

            return true;
        }

        private static object CopyIntoDictionary<TKey, TValue>(object targetObject, object sourceObject)
        {
            var target = (Dictionary<TKey, TValue>) targetObject;

            target.Clear();
            foreach (var pair in (Dictionary<TKey, TValue>) sourceObject)
            {
                target.Add((TKey) Clone(pair.Key), (TValue) Clone(pair.Value));
            }

            return target;
        }

        private static bool ElementsAreEqual<T>(IList<T> a, IList<T> b, int count)
        {
            if (PlainValue<T>.Is)
            {
                var comparer = EqualityComparer<T>.Default;
                for (var i = 0; i < count; i++)
                {
                    if (!comparer.Equals(a[i], b[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            for (var i = 0; i < count; i++)
            {
                if (!AreEqual(a[i], b[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static void CopyElementsInto<T>(IList<T> target, IList<T> source, int count)
        {
            var isPlain = PlainValue<T>.Is;

            for (var i = 0; i < count; i++)
            {
                target[i] = isPlain ? source[i] : (T) CopyInto(target[i], source[i]);
            }
        }

        #endregion Collections
    }
}
#endif
