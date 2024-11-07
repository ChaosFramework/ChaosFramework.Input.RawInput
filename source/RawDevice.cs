using ChaosUtil.Platform.Windows.WinAPI.winuser;
using System;
using System.Windows.Forms;

namespace ChaosFramework.Input.Windows
{
    public abstract class RawDevice : InputDevice
    {
        public RawDevice(InputContext parent)
            : base(parent)
        { }

        public IntPtr deviceHandle;
        public string registryDeviceName;
        public string registryDeviceClass;
        public string source;

        public override string deviceName => registryDeviceName;
        public override string productName => registryDeviceClass;

        internal abstract void ProcessRaw(RAWINPUT raw, Message message, IntPtr buffer);
        internal abstract void Init(RID_DEVICE_INFO info);
    }
}
