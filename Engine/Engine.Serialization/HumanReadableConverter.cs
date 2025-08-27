using System.Reflection;

namespace Engine.Serialization {
    public static class HumanReadableConverter {
        static Dictionary<Type, IHumanReadableConverter> m_humanReadableConvertersByType = [];

        static HashSet<Assembly> m_scannedAssemblies = [];

        public static string ConvertToString(object value) {
            Type type = value.GetType();
            try {
                return GetConverter(type, true).ConvertToString(value);
            }
            catch (Exception innerException) {
                throw new InvalidOperationException($"Cannot convert value of type \"{type.FullName}\" to string.", innerException);
            }
        }

        public static bool TryConvertFromString(Type type, string data, out object result) {
            try {
                result = GetConverter(type, true).ConvertFromString(type, data);
                return true;
            }
            catch (Exception) {
                result = null;
                return false;
            }
        }

        public static bool TryConvertFromString<T>(string data, out T result) {
            if (TryConvertFromString(typeof(T), data, out object result2)) {
                result = (T)result2;
                return true;
            }
            result = default;
            return false;
        }

        public static object ConvertFromString(Type type, string data) {
            try {
                return GetConverter(type, true).ConvertFromString(type, data);
            }
            catch (Exception innerException) {
                throw new InvalidOperationException($"Cannot convert string \"{data}\" to value of type \"{type.FullName}\".", innerException);
            }
        }

        public static T ConvertFromString<T>(string data) => (T)ConvertFromString(typeof(T), data);

        public static bool IsTypeSupported(Type type) => GetConverter(type, false) != null;

        public static string ValuesListToString<T>(char separator, params T[] values) {
            string[] array = new string[values.Length];
            for (int i = 0; i < values.Length; i++) {
                array[i] = ConvertToString(values[i]);
            }
            return string.Join(separator.ToString(), array);
        }

        public static T[] ValuesListFromString<T>(char separator, string data) {
            if (!string.IsNullOrEmpty(data)) {
#if ANDROID
                string[] array = data.Split([separator], StringSplitOptions.None);
#else
                string[] array = data.Split(separator);
#endif
                T[] array2 = new T[array.Length];
                for (int i = 0; i < array.Length; i++) {
                    array2[i] = ConvertFromString<T>(array[i]);
                }
                return array2;
            }
            return [];
        }

        static IHumanReadableConverter GetConverter(Type type, bool throwIfNotFound) {
            ArgumentNullException.ThrowIfNull(type);
            lock (m_humanReadableConvertersByType) {
                if (!m_humanReadableConvertersByType.TryGetValue(type, out IHumanReadableConverter value)) {
                    ScanAssembliesForConverters();
                    if (!m_humanReadableConvertersByType.TryGetValue(type, out value)) {
                        foreach (KeyValuePair<Type, IHumanReadableConverter> item in m_humanReadableConvertersByType) {
                            if (type.GetTypeInfo().IsSubclassOf(item.Key)) {
                                value = item.Value;
                                break;
                            }
                        }
                        m_humanReadableConvertersByType.Add(type, value);
                    }
                }
                return value
                    ?? (throwIfNotFound
                        ? throw new InvalidOperationException(
                            $"IHumanReadableConverter for type \"{type.FullName}\" not found in any loaded assembly."
                        )
                        : null);
            }
        }

        static void ScanAssembliesForConverters() {
            foreach (Assembly item in TypeCache.LoadedAssemblies.Where(a => !TypeCache.IsKnownSystemAssembly(a))) {
                if (!m_scannedAssemblies.Contains(item)) {
                    foreach (TypeInfo definedType in item.DefinedTypes) {
                        HumanReadableConverterAttribute customAttribute = definedType.GetCustomAttribute<HumanReadableConverterAttribute>();
                        if (customAttribute != null) {
                            Type[] types = customAttribute.Types;
                            foreach (Type key in types) {
                                if (!m_humanReadableConvertersByType.ContainsKey(key)) {
                                    IHumanReadableConverter value = (IHumanReadableConverter)Activator.CreateInstance(definedType.AsType());
                                    m_humanReadableConvertersByType.Add(key, value);
                                }
                            }
                        }
                    }
                    m_scannedAssemblies.Add(item);
                }
            }
        }
    }
}