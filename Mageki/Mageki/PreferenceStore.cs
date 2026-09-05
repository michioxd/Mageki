using System;

namespace Mageki
{
    public interface IPreferenceStore
    {
        bool Get(string key, bool defaultValue);
        int Get(string key, int defaultValue);
        float Get(string key, float defaultValue);
        string Get(string key, string defaultValue);
        void Set(string key, bool value);
        void Set(string key, int value);
        void Set(string key, float value);
        void Set(string key, string value);
    }

    public static class PreferenceStore
    {
        private sealed class XamarinPreferenceStore : IPreferenceStore
        {
            public bool Get(string key, bool value) =>
                Xamarin.Essentials.Preferences.Get(key, value);

            public int Get(string key, int value) => Xamarin.Essentials.Preferences.Get(key, value);

            public float Get(string key, float value) =>
                Xamarin.Essentials.Preferences.Get(key, value);

            public string Get(string key, string value) =>
                Xamarin.Essentials.Preferences.Get(key, value);

            public void Set(string key, bool value) =>
                Xamarin.Essentials.Preferences.Set(key, value);

            public void Set(string key, int value) =>
                Xamarin.Essentials.Preferences.Set(key, value);

            public void Set(string key, float value) =>
                Xamarin.Essentials.Preferences.Set(key, value);

            public void Set(string key, string value) =>
                Xamarin.Essentials.Preferences.Set(key, value);
        }

        public static IPreferenceStore Current { get; set; } = new XamarinPreferenceStore();

        public static bool Get(string key, bool value) => Current.Get(key, value);

        public static int Get(string key, int value) => Current.Get(key, value);

        public static float Get(string key, float value) => Current.Get(key, value);

        public static string Get(string key, string value) => Current.Get(key, value);

        public static void Set(string key, bool value) => Current.Set(key, value);

        public static void Set(string key, int value) => Current.Set(key, value);

        public static void Set(string key, float value) => Current.Set(key, value);

        public static void Set(string key, string value) => Current.Set(key, value);
    }
}
