using ChaosUtil.Platform.Windows.WinAPI.winuser;
using Linearstar.Windows.RawInput.Native;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ChaosFramework.Input.Windows
{
    using InputEvents;

    public class HidDevice : RawDevice
    {
        public abstract class HidAxis : InputAxis
        {
            public readonly uint hidUsageID;
            public readonly ushort usagePage, usageIndex;

            internal HidAxis(InputDevice parent, ushort usagePage, ushort usageIndex)
                : base(parent)
            {
                this.usagePage = usagePage;
                this.usageIndex = usageIndex;
                hidUsageID = (uint)(usagePage << 16) | usageIndex;
            }

            public override string GetAxisString()
            {
                switch (usagePage)
                {
                    case 0x9: return "Button" + usageIndex.ToString("X2");
                    case 0xC: return "Media" + usageIndex.ToString("X2");
                    default: return usagePage.ToString("X2") + usageIndex.ToString("X2");
                }
            }
        }

        public sealed class HidButtonAxis : HidAxis
        {
            internal bool nextDown;
            internal bool wasDown;

            internal HidButtonAxis(InputDevice parent, ushort usagePage, ushort usageIndex)
                : base(parent, usagePage, usageIndex)
            { }

            protected override void Update(object data) => value = nextDown ? 1 : 0;
        }

        public sealed class HidValueAxis : HidAxis
        {
            public readonly bool invert;
            public readonly bool clampToZero;

            internal bool receivedValue = false;
            internal int internalValue;

            HidPValueCaps caps;
            bool useReference = false;
            int reference;

            internal HidValueAxis(
                InputDevice parent,
                HidPValueCaps caps,
                ushort usageIndex,
                bool invert,
                bool clampToZero
                ) : base(parent, caps.UsagePage, usageIndex)
            {
                this.caps = caps;
                this.invert = invert;
                this.clampToZero = clampToZero;
            }

            internal HidValueAxis(
                InputDevice parent,
                HidPValueCaps caps,
                ushort usageIndex,
                int referenceValue
                ) : base(parent, caps.UsagePage, usageIndex)
            {
                useReference = true;
                this.caps = caps;
                this.reference = referenceValue;
            }

            internal float GetMappedValue(float v)
            {
                if (useReference)
                    return internalValue == reference ? 1 : 0;
                else
                {
                    float result = caps.Units == 0
                        ? (float)internalValue / (1 << (caps.BitSize - 1)) - 1
                        : (float)internalValue / Math.Max(caps.LogicalMax, -caps.LogicalMin);

                    if (invert)
                        result *= -1;

                    if (clampToZero)
                        result = Math.Max(0, result);

                    return result;
                }
            }

            protected override void Update(object data)
            {
                if (!receivedValue)
                    return;

                value = GetMappedValue(internalValue);
            }

            public override string GetAxisString()
                => useReference
                   ? $"{base.GetAxisString()}(ref {reference})"
                   : $"{base.GetAxisString()}({(invert ? "-" : "+")})";
        }

        RID_DEVICE_INFO info;
        HidPButtonCaps[] buttonCaps;
        HidPValueCaps[] valueCaps;
        HidPreparsedData pPreparsedData;
        string productString;

        public override string productName => productString;
        public uint productIdentifier => (uint)((ushort)info.hid.dwVendorId << 16) | (ushort)info.hid.dwProductId;

        public HidDevice(InputContext parent)
            : base(parent)
        { }

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
                                    AddAxis(
                                        (uint)((btn.UsagePage << 16) | i),
                                        new HidButtonAxis(this, btn.UsagePage, (ushort)i)
                                        );
                            else
                                AddAxis(
                                    (uint)((btn.UsagePage << 16) | btn.NotRange.Usage),
                                    new HidButtonAxis(this, btn.UsagePage, btn.NotRange.Usage)
                                    );

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
            {
                Collections.LinkedList<InputAxis> axes = new Collections.LinkedList<InputAxis>();
                for (int i = val.LogicalMin; i <= val.LogicalMax; i++)
                    axes.Add(new HidValueAxis(this, val, usage, i));

                AddAxis(usageAndPage, axes);
            }
            else
                AddAxis(usageAndPage, new[] {
                    new HidValueAxis(this, val, usage, false, true),
                    new HidValueAxis(this, val, usage, true, true)
                    });
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

                    for (int usage = usageMin; usage <= usageMax; usage++)
                    {
                        InputAxisCollection axis = this[(uint)((buttonCaps[i].UsagePage << 16) | usage)];
                        if (axis != null)
                            ((HidButtonAxis)axis.first).nextDown = false;
                    }

                    foreach (ushort usage in usages)
                    {
                        InputAxisCollection axis = this[(uint)((buttonCaps[i].UsagePage << 16) | usage)];
                        if (axis != null)
                            ((HidButtonAxis)axis.first).nextDown = true;
                    }

                    for (int usage = usageMin; usage <= usageMax; usage++)
                    {
                        InputAxisCollection axisCollection = this[(uint)((buttonCaps[i].UsagePage << 16) | usage)];
                        if (axisCollection == null)
                            continue;

                        HidButtonAxis axis = (HidButtonAxis)axisCollection.first;
                        if (axis.nextDown && !axis.wasDown)
                            AddEvent(new InputPushEvent<HidButtonAxis>(axis, 0, 1));
                        else if (!axis.nextDown && axis.wasDown)
                            AddEvent(new InputReleaseEvent<HidButtonAxis>(axis, 1, 0));

                        axis.wasDown = axis.nextDown;
                    }
                }

            for (int i = 0; i < valueCaps.Length; i++)
                if (valueCaps[i].IsRange)
                    for (int usage = valueCaps[i].Range.UsageMin; usage <= valueCaps[i].Range.UsageMax; usage++)
                    {
                        int value;
                        if (TryGetUsageValue(valueCaps[i], (ushort)usage, rawData, out value) == NtStatus.Success)
                            foreach (HidValueAxis axis in this[(uint)((valueCaps[i].UsagePage << 16) | usage)])
                                ProcessValueAxisChange(axis, value, valueCaps[i].IsAbsolute);
                    }
                else
                {
                    int value;
                    if (TryGetUsageValue(valueCaps[i], valueCaps[i].NotRange.Usage, rawData, out value) == NtStatus.Success)
                    {
                        InputAxisCollection axes = this[(uint)((valueCaps[i].UsagePage << 16) | valueCaps[i].NotRange.Usage)];
                        if (axes != null)
                            foreach (HidValueAxis axis in axes)
                                ProcessValueAxisChange(axis, value, valueCaps[i].IsAbsolute);
                    }
                }
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

        void ProcessValueAxisChange(HidValueAxis axis, int value, bool isAbsolute)
        {
            float oldValue = axis.GetMappedValue(axis.internalValue);
            if (isAbsolute)
                axis.internalValue = value;
            else
                axis.internalValue += value;

            axis.receivedValue = true;

            float newValue = axis.GetMappedValue(axis.internalValue);
            if ((oldValue < axis.pushThreshold && newValue >= axis.pushThreshold)
                || (oldValue > -axis.pushThreshold && newValue <= -axis.pushThreshold)
               )
                AddEvent(new InputPushEvent<HidValueAxis>(axis, oldValue, newValue));
            else if ((oldValue >= axis.pushThreshold && newValue < axis.pushThreshold)
                     || (oldValue <= -axis.pushThreshold && newValue > -axis.pushThreshold)
                    )
                AddEvent(new InputReleaseEvent<HidValueAxis>(axis, oldValue, newValue));
            else if (newValue != oldValue) // we are not pushing a change event if we detected either a push or release
                AddEvent(new InputChangeEvent<HidValueAxis>(axis, oldValue, newValue));
        }
    }
}
