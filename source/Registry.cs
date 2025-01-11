using System.Text.RegularExpressions;
using Win32 = Microsoft.Win32;

namespace ChaosFramework.Input.RawInput
{
    static class Registry
    {
        static class DeviceMatcher
        {
            const RegexOptions OPTIONS = RegexOptions.Compiled | RegexOptions.IgnoreCase;

            const string HEX = "[A-F0-9]";
            const string GUID = HEX + "{8}-(" + HEX + "{4}-){3}" + HEX + "{12}";
            const string DEVICE_ID = @"^[\\\?]{4}([^#]+)#([^#]+)#([^#]+)#\{" + GUID + "\\}$";

            public static readonly Regex REGEX = new Regex(DEVICE_ID, OPTIONS);
        }

        internal static bool TryGetDevice(string item, out string deviceClass, out string deviceDesc)
        {
            Match match = DeviceMatcher.REGEX.Match(item);
            if (match.Success)
            {
                GroupCollection groups = match.Groups;
                Win32.RegistryKey deviceKey = Win32.Registry.LocalMachine.OpenSubKey(
                    $@"System\CurrentControlSet\Enum\{groups[1].Value}\{groups[2].Value}\{groups[3].Value}",
                    false
                    );

                deviceClass = (string)deviceKey.GetValue("Class");
                deviceDesc = (string)deviceKey.GetValue("DeviceDesc");
                return true;
            }
            else
            {
                deviceClass = null;
                deviceDesc = null;
                return false;
            }
        }
    }
}
