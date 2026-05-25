using ChaosUtil.Platform.Windows.WinAPI.winuser;
using Linearstar.Windows.RawInput.Native;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ChaosFramework.Input.RawInput
{
    using Axis;
    using Axis.Hid;
    using System.Collections.Generic;

    public class RawHidDevice : HidDevice
    {
        public sealed class RawButtonAxis : HidButtonAxis
        {
            internal RawButtonAxis(InputDevice parent, HidAxisInfo info)
                : base(parent, info)
            { }

            internal void ProcessChange(bool down)
               => SetDown<RawButtonAxis>(down);
        }

        public sealed class RawBoundedValueAxis : HidBoundedValueAxis
        {
            public readonly bool invert;

            HidPValueCaps caps;

            internal RawBoundedValueAxis(
                InputDevice parent,
                HidPValueCaps caps,
                ushort usageIndex,
                bool invert)
                : base(parent, new HidAxisInfo(caps.UsagePage, usageIndex))
            {
                this.caps = caps;
                this.invert = invert;
            }

            internal void ProcessChange(float change)
                => SetValue<RawBoundedValueAxis>(GetMappedValue(change));

            internal float GetMappedValue(float rawValue)
            {
                float result = caps.Units == 0
                    ? (float)rawValue / (1 << (caps.BitSize - 1)) - 1
                    : (float)rawValue / Math.Max(caps.LogicalMax, -caps.LogicalMin);

                if (invert)
                    result *= -1;

                result = Math.Max(0, result);

                return result;
            }

            public override string GetAxisString()
                => $"{base.GetAxisString()}({(invert ? "-" : "+")})";
        }

        public sealed class RawUnboundedValueAxis : HidValueAxis
        {
            readonly HidAxisInfo info;

            HidPValueCaps caps;

            internal RawUnboundedValueAxis(
                InputDevice parent,
                HidPValueCaps caps,
                ushort usageIndex)
                : base(parent, new HidAxisInfo(caps.UsagePage, usageIndex))
            {
                this.caps = caps;
            }

            internal void ProcessChange(float change)
                => SetValue<RawBoundedValueAxis>(GetMappedValue(change));

            internal float GetMappedValue(float rawValue)
            {
                float result = caps.Units == 0
                    ? (float)rawValue / (1 << (caps.BitSize - 1)) - 1
                    : (float)rawValue / Math.Max(caps.LogicalMax, -caps.LogicalMin);

                return result;
            }
        }

        public sealed class RawHidPov : Controller.AxisArea
        {
            static AxisFactory CreateOwnedAxis(InputDevice parent, HidAxisInfo info)
                => (x, positive) => new RawPovAxis(parent, info, x, positive ? 1 : -1);

            int numValues;

            public RawHidPov(InputDevice parent, HidAxisInfo info, int numValues)
                : base(CreateOwnedAxis(parent, info))
            {
                this.numValues = numValues;
            }

            internal void Update(int value)
            {
                // TODO: compare with the HID spec
                // 0 means the rest position, [1; MaxValue] are the clockwise angle values
                // Also this is confusing because it's actually a true diagonal flip. Document if necessary !?
                if (value == 0)
                {
                    foreach (RawPovAxis axis in EnumerateAxes())
                        axis.Update(0, 0);
                }
                else
                {
                    float angle = (float)System.Math.PI * 2 * (value - 1) / numValues; // TODO: maxValue instead of numValues
                    float rawX = (float)Math.Sin(angle);
                    float rawY = (float)Math.Cos(angle);
                    foreach (RawPovAxis axis in EnumerateAxes())
                        axis.Update(rawX, rawY);
                }
            }
        }

        public sealed class RawPovAxis : HidBoundedValueAxis
        {
            bool x;
            int sign;

            internal RawPovAxis(InputDevice parent, HidAxisInfo hidInfo, bool x, int sign)
                : base(parent, hidInfo)
            {
                this.x = x;
                this.sign = sign;
            }

            internal void Update(float rawX, float rawY)
            {
                float effectiveValue = (x ? rawX : rawY) * sign;
                SetValue<RawPovAxis>(effectiveValue > 0 ? effectiveValue : 0);
            }

            public override string GetAxisString()
                => $"{hidInfo.GetAxisString()} {(x ? "X" : "Y")} {(sign > 0 ? "positive" : "negative")}";
        }

        internal sealed class Implementation(RawHidDevice parent)
            : RawDevice(parent)
        {
            internal RID_DEVICE_INFO info;
            HidPButtonCaps[] buttonCaps;
            HidPValueCaps[] valueCaps;
            HidPreparsedData pPreparsedData;
            internal string productString;

            readonly RawHidDevice parent = parent;

            internal override void Init(RID_DEVICE_INFO info)
            {
                this.info = info;

                HidDeviceHandle hidHandle;
                if (HidD.TryOpenDevice(source, out hidHandle))
                {
                    productString = HidD.GetProductString(hidHandle);
                    HidD.CloseDevice(hidHandle);
                }

                uint cbSize = 0;
                GetRawInputDeviceInfo.Invoke(
                    deviceHandle,
                    GetRawInputDeviceInfo.Command.RIDI_PREPARSEDDATA,
                    IntPtr.Zero,
                    ref cbSize
                    );

                pPreparsedData = (HidPreparsedData)Marshal.AllocHGlobal((IntPtr)cbSize);
                GetRawInputDeviceInfo.Invoke(
                    deviceHandle,
                    GetRawInputDeviceInfo.Command.RIDI_PREPARSEDDATA,
                    HidPreparsedData.GetRawValue(pPreparsedData),
                    ref cbSize
                    );

                if (pPreparsedData != HidPreparsedData.Zero)
                {
                    if (HidP.TryGetButtonCaps(pPreparsedData, HidPReportType.Input, out buttonCaps) == NtStatus.Success)
                        foreach (HidPButtonCaps btn in buttonCaps)
                            if (btn.UsagePage != 0xff00)
                                if (btn.IsRange)
                                    for (int i = btn.Range.UsageMin; i <= btn.Range.UsageMax; i++)
                                        parent.buttonAxes[(uint)((btn.UsagePage << 16) | i)]
                                            = new RawButtonAxis(parent, new HidAxisInfo(btn.UsagePage, (ushort)i));
                                else
                                    parent.buttonAxes[(uint)((btn.UsagePage << 16) | btn.NotRange.Usage)]
                                        = new RawButtonAxis(parent, new HidAxisInfo(btn.UsagePage, btn.NotRange.Usage));

                    if (HidP.TryGetValueCaps(pPreparsedData, HidPReportType.Input, out valueCaps) == NtStatus.Success)
                        foreach (HidPValueCaps val in valueCaps)
                            if (val.UsagePage != 0xff00)
                                if (val.IsRange)
                                    for (int i = val.Range.UsageMin; i <= val.Range.UsageMax; i++)
                                        MakeSeperateAxis(val, (ushort)i);
                                else
                                    MakeSeperateAxis(val, val.NotRange.Usage);
                }
            }

            void MakeSeperateAxis(HidPValueCaps val, ushort usage)
            {
                uint usageAndPage = (uint)((val.UsagePage << 16) | usage);
                if ((val.BitField & 0x40) != 0) // POV controller
                    parent.povs[usageAndPage] = new RawHidPov(parent, new HidAxisInfo(val.UsagePage, usage), val.LogicalMax - val.LogicalMin + 1);
                else
                {
                    if (val.IsAbsolute)
                        parent.boundedValueAxes[usageAndPage] = new[]
                        {
                            new RawBoundedValueAxis(parent, val, usage, false),
                            new RawBoundedValueAxis(parent, val, usage, true),
                        };
                    else
                        parent.unboundedValueAxes[usageAndPage] = new RawUnboundedValueAxis(parent, val, usage);
                }
            }

            internal override void ProcessRaw(RAWINPUT raw, Message message, IntPtr buffer)
            {
                RAWHID rawHid = RAWHID.FromHandle(IntPtr.Add(buffer, Marshal.SizeOf(typeof(RAWINPUTHEADER))));
                byte[] rawData = rawHid.GetRawData();
                ushort[] usages;

                for (int i = 0; i < buttonCaps.Length; i++)
                    if (TryGetUsages(buttonCaps[i], rawData, out usages) == NtStatus.Success)
                    {
                        int usageMin = buttonCaps[i].IsRange ? buttonCaps[i].Range.UsageMin : buttonCaps[i].NotRange.Usage;
                        int usageMax = buttonCaps[i].IsRange ? buttonCaps[i].Range.UsageMax : buttonCaps[i].NotRange.Usage;

                        bool[] down = new bool[usageMax - usageMin + 1];

                        foreach (ushort usage in usages)
                            down[usage - usageMin] = true;

                        for (int usage = usageMin; usage <= usageMax; usage++)
                        {
                            RawButtonAxis axis = parent.buttonAxes[(uint)((buttonCaps[i].UsagePage << 16) | usage)];
                            axis.ProcessChange(down[usage - usageMin]);
                        }
                    }

                for (int i = 0; i < valueCaps.Length; i++)
                    if (valueCaps[i].IsRange)
                        for (int usage = valueCaps[i].Range.UsageMin; usage <= valueCaps[i].Range.UsageMax; usage++)
                        {
                            int value;
                            if (TryGetUsageValue(valueCaps[i], (ushort)usage, rawData, out value) == NtStatus.Success)
                                UpdateValueAxes((uint)((valueCaps[i].UsagePage << 16) | usage), value);
                        }
                    else
                    {
                        int value;
                        if (TryGetUsageValue(valueCaps[i], valueCaps[i].NotRange.Usage, rawData, out value) == NtStatus.Success)
                            UpdateValueAxes((uint)((valueCaps[i].UsagePage << 16) | valueCaps[i].NotRange.Usage), value);
                    }
            }

            void UpdateValueAxes(uint usageAndPage, int value)
            {
                RawUnboundedValueAxis unbounded;
                if (parent.unboundedValueAxes.TryGetValue(usageAndPage, out unbounded))
                    unbounded.ProcessChange(value);

                RawBoundedValueAxis[] bounded;
                if (parent.boundedValueAxes.TryGetValue(usageAndPage, out bounded))
                    foreach (RawBoundedValueAxis subaxis in bounded)
                        subaxis.ProcessChange(value);

                RawHidPov pov;
                if (parent.povs.TryGetValue(usageAndPage, out pov))
                    pov.Update(value);
            }

            NtStatus TryGetUsages(HidPButtonCaps buttonCaps, byte[] rawData, out ushort[] usages)
                => HidP.TryGetUsages(
                    pPreparsedData,
                    HidPReportType.Input,
                    buttonCaps.UsagePage,
                    buttonCaps.LinkCollection,
                    rawData,
                    rawData.Length,
                    out usages
                    );

            NtStatus TryGetUsageValue(HidPValueCaps valueCaps, ushort usage, byte[] rawData, out int usageValue)
                => HidP.TryGetUsageValue(
                    pPreparsedData,
                    HidPReportType.Input,
                    valueCaps.UsagePage,
                    valueCaps.LinkCollection,
                    usage,
                    rawData,
                    rawData.Length,
                    out usageValue
                    );
        }

        internal readonly Implementation implementation;

        readonly Dictionary<uint, RawButtonAxis> buttonAxes = new Dictionary<uint, RawButtonAxis>();
        readonly Dictionary<uint, RawHidPov> povs = new Dictionary<uint, RawHidPov>();
        readonly Dictionary<uint, RawBoundedValueAxis[]> boundedValueAxes = new Dictionary<uint, RawBoundedValueAxis[]>();
        readonly Dictionary<uint, RawUnboundedValueAxis> unboundedValueAxes = new Dictionary<uint, RawUnboundedValueAxis>();

        public override string productName => implementation.productString;
        public uint productIdentifier => (uint)((ushort)implementation.info.hid.dwVendorId << 16) | (ushort)implementation.info.hid.dwProductId;

        public RawHidDevice(InputContext parent)
            : base(parent)
        {
            implementation = new Implementation(this);
        }

        public override IEnumerator<InputAxis> GetEnumerator()
        {
            foreach (RawButtonAxis axis in buttonAxes.Values)
                yield return axis;

            foreach (RawHidPov pov in povs.Values)
                foreach (RawPovAxis axis in pov.EnumerateAxes())
                    yield return axis;

            foreach (RawBoundedValueAxis[] axes in boundedValueAxes.Values)
                foreach (RawBoundedValueAxis axis in axes)
                    yield return axis;

            foreach (RawUnboundedValueAxis axis in unboundedValueAxes.Values)
                yield return axis;
        }

        protected override InputAxis GetByUsageInternal(HidPage hidPage, ushort hidUsage, int subIndex)
        {
            throw new NotImplementedException();
        }

        public override bool IsConnected()
            => true;
    }
}
