﻿using System.Dynamic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Juice.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Juice.Models
{
    public class DynamicModel : DynamicObject, IDynamic
    {
        #region Dynamic

        public virtual JObject Properties { get; set; } = new JObject();

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public virtual Dictionary<string, object?> OriginalPropertyValues { get; set; } = [];

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public virtual Dictionary<string, object?> CurrentPropertyValues { get; set; } = [];

        public virtual T? GetProperty<T>(Func<T>? defaultValue = null, [CallerMemberName] string? name = null)
        {
            Validator.NotNullOrWhiteSpace(name, nameof(name));
            var item = Properties[name!];
            return item != null ? item.ToObject<T>() : defaultValue != null ? defaultValue() : default;
        }

        public virtual T? GetProperty<T>(Type type, Func<T>? defaultValue = null, [CallerMemberName] string? name = null)
        {
            Validator.NotNullOrWhiteSpace(name, nameof(name));
            var item = Properties[name!];
            return (T?)item?.ToObject(type) ?? (defaultValue != null ? defaultValue() : default);
        }

        public virtual void SetProperty(object? value, [CallerMemberName] string? name = null)
        {
            Validator.NotNullOrWhiteSpace(name, nameof(name));
            Properties = new JObject(Properties);

            OriginalPropertyValues[name!] = Properties[name!];

            var val = value != null ? JToken.FromObject(value) : JValue.CreateNull();

            CurrentPropertyValues[name!] = val;

            Properties[name!] = val;
        }

        /// <summary>
        /// Reflection Helper method to retrieve a property
        /// </summary>
        /// <param name="name"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        protected bool GetSelfProperty(string name, out object? result)
        {
            var miArray = GetType().GetMember(name, BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Instance);
            if (miArray != null && miArray.Length > 0)
            {
                var mi = miArray[0];
                if (mi.MemberType == MemberTypes.Property)
                {
                    result = ((PropertyInfo)mi).GetValue(this, null);
                    return true;
                }
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Reflection helper method to set a property value
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected bool SetSelfProperty(string name, object? value)
        {
            var miArray = GetType().GetMember(name, BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.Instance);
            if (miArray != null && miArray.Length > 0)
            {
                var mi = miArray[0];
                if (mi.MemberType == MemberTypes.Property)
                {
                    ((PropertyInfo)mi).SetValue(this, value, null);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Convenience method that provides a string Indexer 
        /// to the Properties collection AND the strongly typed
        /// properties of the object by name.
        /// 
        /// // dynamic
        /// exp["Address"] = "112 nowhere lane"; 
        /// // strong
        /// var name = exp["StronglyTypedProperty"] as string; 
        /// </summary>
        /// <remarks>
        /// The getter checks the Properties dictionary first
        /// then looks in PropertyInfo for properties.
        /// The setter checks the instance properties before
        /// checking the Properties dictionary.
        /// </remarks>
        /// <param name="key"></param>
        /// 
        /// <returns></returns>
        public object? this[string key]
        {
            get
            {
                return GetSelfProperty(key, out object? value) ? value : GetProperty<object?>(() => null, key);
            }
            set
            {
                if (!SetSelfProperty(key, value))
                {
                    SetProperty(value, key);
                    return;
                }
            }
        }

        // If you try to get a value of a property
        // not defined in the class, this method is called.
        public override bool TryGetMember(
            GetMemberBinder binder, out object? result)
        {
            // Converting the property name to lowercase
            // so that property names become case-insensitive.

            // If the property name is found in a dictionary,
            // set the result parameter to the property value and return true.
            // Otherwise, return false.
            result = GetProperty<object?>(default, binder.Name);
            return true;
        }

        // If you try to set a value of a property that is
        // not defined in the class, this method is called.
        public override bool TrySetMember(
            SetMemberBinder binder, object? value)
        {
            // Converting the property name to lowercase
            // so that property names become case-insensitive.
            SetProperty(value, binder.Name);

            // You can always add a value to a dictionary,
            // so this method always returns true.
            return true;
        }
        #endregion
    }
}
