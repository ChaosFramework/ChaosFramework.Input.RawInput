using ChaosUtil.Platform.Windows.WinAPI.winuser;
using System;
using System.Windows.Forms;

namespace ChaosFramework.Input.RawInput
{
    internal abstract class RawDevice(InputDevice parent)
    {
        internal readonly InputDevice parent = parent;

        public IntPtr deviceHandle;
        public string registryDeviceName;
        public string registryDeviceClass;
        public string source;

        public string deviceName => registryDeviceName;
        public string productName => registryDeviceClass;

        internal abstract void ProcessRaw(RAWINPUT raw, Message message, IntPtr buffer);
        internal abstract void Init(RID_DEVICE_INFO info);
    }
}
