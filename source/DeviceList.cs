using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ChaosUtil.Platform.Windows.WinAPI.winuser;
using Linearstar.Windows.RawInput.Native;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Input.RawInput
{
    using Layouts;

    public sealed class DeviceList : SysCol.IEnumerable<RawDevice>
    {
        enum UsageAndPage : uint
        {
            Mouse = 0x00010002,
            Keyboard = 0x00010006,
            Hid = 0x000A0000,
            UsageMask = 0x0000FFFF,
            PageMask = 0xFFFF0000,
        }

        const string ROOT_DEVICE_NAME = "root";

        static readonly object msgLock = new object();

        static T DoRidiCommand<T>(
            IntPtr hDevice,
            GetRawInputDeviceInfo.Command uiCommand,
            ref uint pcbSize,
            Func<IntPtr, T> convertResult
            )
        {
            IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
            try
            {
                GetRawInputDeviceInfo.Invoke(hDevice, uiCommand, pData, ref pcbSize);
                return convertResult(pData);
            }
            finally
            {
                Marshal.FreeHGlobal(pData);
            }
        }

        readonly InputContext context;
        readonly IntPtr windowHandle;
        readonly SysCol.HashSet<UsageAndPage> registeredDeviceClasses = new SysCol.HashSet<UsageAndPage>();
        readonly SysCol.Dictionary<string, RawDevice> knownDevices = new SysCol.Dictionary<string, RawDevice>();
        readonly SysCol.Dictionary<IntPtr, string> handleToDeviceID = new SysCol.Dictionary<IntPtr, string>();

        internal DeviceList(InputContext context, IntPtr windowHandle)
        {
            this.context = context;
            this.windowHandle = windowHandle;
            RegisterDeviceTypes(new SysCol.HashSet<UsageAndPage> { UsageAndPage.Keyboard, UsageAndPage.Mouse });
        }

        public void UpdateDeviceList()
        {
            SysCol.HashSet<IntPtr> forgottenDevices = new SysCol.HashSet<IntPtr>(handleToDeviceID.Keys);
            SysCol.HashSet<UsageAndPage> newDeviceClasses = new SysCol.HashSet<UsageAndPage>();

            uint uiNumDevices = 0;
            uint cbSize = (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));
            if (GetRawInputDeviceList.Invoke(IntPtr.Zero, ref uiNumDevices, cbSize) == 0)
            {
                IntPtr pRawInputDeviceList = Marshal.AllocHGlobal((int)(cbSize * uiNumDevices));
                try
                {
                    GetRawInputDeviceList.Invoke(pRawInputDeviceList, ref uiNumDevices, cbSize);
                    uint bufferSize = uiNumDevices * cbSize;
                    for (uint deviceOffset = 0; deviceOffset < bufferSize; deviceOffset += cbSize)
                        TryCreateDevice(
                            Marshal.PtrToStructure<RAWINPUTDEVICELIST>(IntPtr.Add(pRawInputDeviceList, (int)deviceOffset)),
                            forgottenDevices,
                            newDeviceClasses
                            );
                }
                finally
                {
                    Marshal.FreeHGlobal(pRawInputDeviceList);
                }
            }
            else
                throw new Exception(
                    "Could not read device list.",
                    new Linearstar.Windows.RawInput.Native.Win32ErrorException()
                    );

            RegisterDeviceTypes(newDeviceClasses);
            foreach (IntPtr forgotten in forgottenDevices)
                handleToDeviceID.Remove(forgotten);
        }

        void TryCreateDevice(
            RAWINPUTDEVICELIST rid,
            SysCol.HashSet<IntPtr> forgottenDevices,
            SysCol.HashSet<UsageAndPage> deviceClassesToRegister
            )
        {
            uint pcbSize = 0;
            GetRawInputDeviceInfo.Invoke(rid.hDevice, GetRawInputDeviceInfo.Command.RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize);
            if (pcbSize == 0)
                return;

            string deviceName = DoRidiCommand(
                rid.hDevice,
                GetRawInputDeviceInfo.Command.RIDI_DEVICENAME,
                ref pcbSize,
                Marshal.PtrToStringAnsi
                );

            if (deviceName.ToLower().Contains(ROOT_DEVICE_NAME))
                return;

            if (forgottenDevices.Contains(rid.hDevice))
            {
                forgottenDevices.Remove(rid.hDevice);
                return;
            }

            uint deviceInfoSize = RID_DEVICE_INFO.SIZE;
            RID_DEVICE_INFO deviceInfo = DoRidiCommand(
                rid.hDevice,
                GetRawInputDeviceInfo.Command.RIDI_DEVICEINFO,
                ref deviceInfoSize,
                Marshal.PtrToStructure<RID_DEVICE_INFO>
                );

            RawDevice rawDevice;
            switch (rid.dwType)
            {
                case RIM_TYPE.KEYBOARD:
                    rawDevice = new Keyboard(context);
                    break;

                case RIM_TYPE.MOUSE:
                    rawDevice = new Mouse(context);
                    break;

                case RIM_TYPE.HID:
                    UsageAndPage page = (UsageAndPage)(deviceInfo.hid.usUsagePage << 16);
                    UsageAndPage usageAndPage = page | (UsageAndPage)deviceInfo.hid.usUsage;
                    if (!registeredDeviceClasses.Contains(usageAndPage) && page != UsageAndPage.Hid)
                        deviceClassesToRegister.Add(usageAndPage);

                    uint product = ((uint)deviceInfo.hid.dwVendorId << 16) | (ushort)deviceInfo.hid.dwProductId;
                    HidLayout layout = context.layoutMgr.GetLayout(product);
                    if (layout != null)
                    {
                        Type deviceType = typeof(MappedHidDevice<>).MakeGenericType(layout.enumType);
                        rawDevice = (MappedHidDevice)Activator.CreateInstance(deviceType, new[] { context });
                    }
                    else
                        rawDevice = new HidDevice(context);
                    break;

                default:
                    return;
            }

            rawDevice.source = deviceName;
            rawDevice.deviceHandle = rid.hDevice;
            if (!Registry.TryGetDevice(deviceName, out rawDevice.registryDeviceClass, out rawDevice.registryDeviceName))
                return;

            rawDevice.Init(deviceInfo);

            string id = $"{rawDevice.deviceName}\n{rawDevice.source}";
            if (!knownDevices.ContainsKey(id))
                knownDevices[id] = rawDevice;

            handleToDeviceID[rawDevice.deviceHandle] = id;
        }

        void RegisterDeviceTypes(SysCol.HashSet<UsageAndPage> usages)
        {
            if (usages.Count <= 0)
                return;

            RAWINPUTDEVICE[] newDevices = new RAWINPUTDEVICE[usages.Count];

            int i = 0;
            foreach (UsageAndPage usageAndPage in usages)
            {
                newDevices[i++] = new RAWINPUTDEVICE(
                    (ushort)((uint)usageAndPage >> 16),
                    (ushort)(usageAndPage & UsageAndPage.UsageMask),
                    RAWINPUTDEVICE.RIDEV.INPUTSINK,
                    windowHandle
                    );
                registeredDeviceClasses.Add(usageAndPage);
            }

            if (!RegisterRawInputDevices.Invoke(newDevices))
            {
                Win32ErrorException ex = new Win32ErrorException();
                if ((uint)ex.HResult != 0x80131500) // The operation completed successfully
                    throw ex;
            }
        }

        public void ProcessMessage(Message message)
        {
            lock (msgLock)
            {
                uint pcbSize = 0;
                GetRawInputData.Invoke(
                    message.LParam,
                    GetRawInputData.Command.RID_INPUT,
                    IntPtr.Zero,
                    ref pcbSize,
                    (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))
                    );

                IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
                try
                {
                    if (GetRawInputData.Invoke(
                        message.LParam,
                        GetRawInputData.Command.RID_INPUT,
                        pData,
                        ref pcbSize,
                        (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))
                        ) == pcbSize)
                    {
                        RAWINPUT rawInput = Marshal.PtrToStructure<RAWINPUT>(pData);

                        string deviceID;
                        if (handleToDeviceID.TryGetValue(rawInput.header.hDevice, out deviceID))
                        {
                            RawDevice knownDevice;
                            if (knownDevices.TryGetValue(deviceID, out knownDevice))
                                knownDevice.ProcessRaw(rawInput, message, pData);
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pData);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        SysCol.IEnumerator<RawDevice> SysCol.IEnumerable<RawDevice>.GetEnumerator() => GetEnumerator();
        public SysCol.IEnumerator<RawDevice> GetEnumerator() => knownDevices.Values.GetEnumerator();
    }
}
