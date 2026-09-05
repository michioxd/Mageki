using System.Globalization;
using Microsoft.Win32;

namespace Mageki.WPF
{
    public sealed class WpfPreferenceStore : IPreferenceStore
    {
        private const string RegistryPath = @"Software\Mageki";

        private static RegistryKey OpenKey(bool writable = false) =>
            Registry.CurrentUser.CreateSubKey(RegistryPath, writable);

        public bool Get(string key, bool defaultValue) =>
            System.Convert.ToBoolean(OpenKey().GetValue(key, defaultValue ? 1 : 0));

        public int Get(string key, int defaultValue) =>
            System.Convert.ToInt32(OpenKey().GetValue(key, defaultValue));

        public float Get(string key, float defaultValue) =>
            float.TryParse(
                OpenKey()
                    .GetValue(key, defaultValue.ToString(CultureInfo.InvariantCulture))
                    ?.ToString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var value
            )
                ? value
                : defaultValue;

        public string Get(string key, string defaultValue) =>
            OpenKey().GetValue(key, defaultValue)?.ToString() ?? defaultValue;

        public void Set(string key, bool value) =>
            OpenKey(true).SetValue(key, value ? 1 : 0, RegistryValueKind.DWord);

        public void Set(string key, int value) =>
            OpenKey(true).SetValue(key, value, RegistryValueKind.DWord);

        public void Set(string key, float value) =>
            OpenKey(true)
                .SetValue(
                    key,
                    value.ToString(CultureInfo.InvariantCulture),
                    RegistryValueKind.String
                );

        public void Set(string key, string value) =>
            OpenKey(true).SetValue(key, value ?? string.Empty, RegistryValueKind.String);
    }
}
