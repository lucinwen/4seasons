using UnityEngine;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
#endif

internal static class WorkshopLegacyPreferences
{
    internal static void Import()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        const string marker = "ShiXu.Settings.ProductNameMigrated";
        if (PlayerPrefs.HasKey(marker)) return;

#if UNITY_EDITOR_WIN
        const string prefix = @"Software\Unity\UnityEditor\";
#else
        const string prefix = @"Software\";
#endif
        string previous = prefix + Application.companyName + @"\江南初冬·天然染料采集";
        bool copied = CopyString(previous, "ShiXu.Workshop.v1");
        copied |= CopyString(previous, "ShiXu.PostOffice.v1");

        const string fullscreen = "ShiXu.Settings.Fullscreen";
        if (!PlayerPrefs.HasKey(fullscreen))
        {
            byte[] data = Read(previous, fullscreen, out _, out int length);
            if (length == sizeof(int))
            {
                PlayerPrefs.SetInt(fullscreen, BitConverter.ToInt32(data, 0) == 0 ? 0 : 1);
                copied = true;
            }
        }

        const string volume = "ShiXu.Settings.MasterVolume";
        if (!PlayerPrefs.HasKey(volume))
        {
            byte[] data = Read(previous, volume, out _, out int length);
            if (length == sizeof(float))
            {
                float value = BitConverter.ToSingle(data, 0);
                if (value >= 0f && value <= 1f)
                {
                    PlayerPrefs.SetFloat(volume, value);
                    copied = true;
                }
            }
        }

        if (copied)
        {
            PlayerPrefs.SetInt(marker, 1);
            PlayerPrefs.Save();
        }
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private static readonly IntPtr CurrentUser = new IntPtr(unchecked((int)0x80000001));
    private const uint AnyType = 0x0000ffff;
    private const uint RegistryString = 1;
    private const uint RegistryBinary = 3;

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int RegGetValueW(IntPtr key, string subkey, string name, uint flags,
        out uint type, [Out] byte[] data, ref int bytes);

    private static bool CopyString(string previous, string key)
    {
        if (PlayerPrefs.HasKey(key)) return false;
        byte[] data = Read(previous, key, out uint type, out int length);
        if (length == 0 || (type != RegistryString && type != RegistryBinary)) return false;
        Encoding encoding = type == RegistryString ? Encoding.Unicode : Encoding.UTF8;
        string value = encoding.GetString(data, 0, length).TrimEnd('\0');
        if (value.Length == 0) return false;
        PlayerPrefs.SetString(key, value);
        return true;
    }

    private static byte[] Read(string previous, string key, out uint type, out int length)
    {
        // Windows Unity 2022 stores PlayerPrefs with a DJB2-XOR suffix on the value name.
        uint hash = 5381;
        foreach (char letter in key) hash = unchecked(hash * 33) ^ letter;
        string valueName = key + "_h" + hash.ToString(CultureInfo.InvariantCulture);
        length = 0;
        int status = RegGetValueW(CurrentUser, previous, valueName, AnyType, out type, null, ref length);
        if (status != 0 || length <= 0)
        {
            length = 0;
            return Array.Empty<byte>();
        }

        var data = new byte[length];
        if (RegGetValueW(CurrentUser, previous, valueName, AnyType, out type, data, ref length) == 0) return data;
        length = 0;
        return Array.Empty<byte>();
    }
#endif
}
