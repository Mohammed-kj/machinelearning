﻿// <copyright file="Parameter.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.ML.ModelBuilder.SearchSpace.Converter;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Microsoft.ML.ModelBuilder.SearchSpace
{
    // TODO
    // Add tests
    public class Parameter : IParameter
    {
        private JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            Formatting = Formatting.Indented,
            Culture = System.Globalization.CultureInfo.InvariantCulture,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new JsonConverter[]
            {
                new StringEnumConverter(),
            },
        };

        private object value;

        private Parameter(object value, ParameterType type)
        {
            this.value = value;
            this.ParameterType = type;
        }

        public static Parameter FromDouble(double value)
        {
            return new Parameter(value, ParameterType.Float);
        }

        public static Parameter FromFloat(float value)
        {
            return new Parameter(value, ParameterType.Float);
        }

        public static Parameter FromInt(int value)
        {
            return new Parameter(value, ParameterType.Integer);
        }

        public static Parameter FromString(string value)
        {
            return new Parameter(value, ParameterType.Integer);
        }

        public static Parameter FromBool(bool value)
        {
            return new Parameter(value, ParameterType.Bool);
        }

        public static Parameter FromEnum<T>(T value) where T: struct, Enum
        {
            return Parameter.FromEnum(value, typeof(T));
        }

        public static Parameter FromIEnumerable<T>(IEnumerable<T> values)
        {
            // check T
            return Parameter.FromIEnumerable(values as IEnumerable);
        }

        private static Parameter FromIEnumerable(IEnumerable values)
        {
            return new Parameter(values, ParameterType.Array);
        }

        private static Parameter FromEnum(Enum e, Type t)
        {
            return Parameter.FromString(Enum.GetName(t, e));
        }

        public static Parameter FromObject<T>(T value) where T: class
        {
            return Parameter.FromObject(value, typeof(T));
        }

        private static Parameter FromObject(object value, Type type)
        {
            var param = value switch
            {
                int i => Parameter.FromInt(i),
                double d => Parameter.FromDouble(d),
                float f => Parameter.FromFloat(f),
                string s => Parameter.FromString(s),
                bool b => Parameter.FromBool(b),
                IEnumerable vs => Parameter.FromIEnumerable(vs),
                Enum e => Parameter.FromEnum(e, e.GetType()),
                _ => null,
            };

            if (param != null)
            {
                return param;
            }
            else
            {
                var parameter = Parameter.CreateNestedParameter();
                var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                        .Where(p => p.CanRead && p.CanWrite);
                foreach (var property in properties)
                {
                    var name = property.Name;
                    var pValue = property.GetValue(value);
                    if (pValue != null)
                    {
                        var _prameter = Parameter.FromObject(pValue, property.PropertyType);

                        if (_prameter?.Count != 0)
                        {
                            parameter[name] = _prameter;
                        }
                    }
                }

                return parameter;
            }
        }

        public static Parameter CreateNestedParameter(params KeyValuePair<string, IParameter>[] parameters)
        {
            var parameter = new Parameter(new Dictionary<string, IParameter>(), ParameterType.Object);
            foreach (var param in parameters)
            {
                parameter[param.Key] = param.Value;
            }

            return parameter;
        }

        public object Value { get => this.value; }

        public int Count => this.ParameterType == ParameterType.Object ? (this.value as Dictionary<string, IParameter>).Count : 1;

        public bool IsReadOnly
        {
            get
            {
                this.VerifyIfParameterIsObjectType();
                return (this.value as IDictionary<string, IParameter>).IsReadOnly;
            }
        }

        public ParameterType ParameterType { get; }

        ICollection<IParameter> IDictionary<string, IParameter>.Values
        {
            get
            {
                this.VerifyIfParameterIsObjectType();
                return (this.value as IDictionary<string, IParameter>).Values;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                this.VerifyIfParameterIsObjectType();
                return (this.value as IDictionary<string, IParameter>).Keys;
            }
        }

        public IParameter this[string key]
        {
            get
            {
                this.VerifyIfParameterIsObjectType();
                return (this.value as IDictionary<string, IParameter>)[key];
            }

            set
            {
                this.VerifyIfParameterIsObjectType();
                (this.value as IDictionary<string, IParameter>)[key] = value;
            }
        }

        public T AsType<T>()
        {
            if (this.value is T t)
            {
                return t;
            }
            else
            {
                var json = JsonConvert.SerializeObject(this.value, this.settings);
                return JsonConvert.DeserializeObject<T>(json, this.settings);
            }
        }

        public void Clear()
        {
            this.VerifyIfParameterIsObjectType();
            (this.value as Dictionary<string, IParameter>).Clear();
        }

        public void Add(string key, IParameter value)
        {
            this.VerifyIfParameterIsObjectType();
            (this.value as Dictionary<string, IParameter>).Add(key, value);
        }

        public bool TryGetValue(string key, out IParameter value)
        {
            this.VerifyIfParameterIsObjectType();
            return (this.value as Dictionary<string, IParameter>).TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<string, IParameter> item)
        {
            this.VerifyIfParameterIsObjectType();
            (this.value as Dictionary<string, IParameter>).Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<string, IParameter> item)
        {
            this.VerifyIfParameterIsObjectType();
            return (this.value as Dictionary<string, IParameter>).Contains(item);
        }

        public bool Remove(KeyValuePair<string, IParameter> item)
        {
            this.VerifyIfParameterIsObjectType();
            return (this.value as IDictionary<string, IParameter>).Remove(item);
        }

        IEnumerator<KeyValuePair<string, IParameter>> IEnumerable<KeyValuePair<string, IParameter>>.GetEnumerator()
        {
            this.VerifyIfParameterIsObjectType();
            return (this.value as IDictionary<string, IParameter>).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            this.VerifyIfParameterIsObjectType();
            return (this.value as IDictionary<string, IParameter>).GetEnumerator();
        }

        private void VerifyIfParameterIsObjectType()
        {
            Contract.Requires(this.ParameterType == ParameterType.Object, "parameter is not object type.");
        }

        public void CopyTo(KeyValuePair<string, IParameter>[] array, int arrayIndex)
        {
            this.VerifyIfParameterIsObjectType();
            (this.value as IDictionary<string, IParameter>).CopyTo(array, arrayIndex);
        }

        public bool ContainsKey(string key)
        {
            this.VerifyIfParameterIsObjectType();
            return (this.value as IDictionary<string, IParameter>).ContainsKey(key);
        }

        public bool Remove(string key)
        {
            this.VerifyIfParameterIsObjectType();
            return (this.value as IDictionary<string, IParameter>).Remove(key);
        }
    }
}
