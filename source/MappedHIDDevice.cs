// TODO: yes
/*
using System;

namespace ChaosFramework.Input.RawInput
{
    using Layouts;

    public abstract class MappedHidDevice : HidDevice
    {
        public readonly Type enumType;
        public abstract HidLayout layout { get; }

        public MappedHidDevice(InputContext parent, Type enumType) : base(parent)
        {
            if (!(this.enumType = enumType).IsEnum)
                throw new InvalidOperationException($"{nameof(enumType)} must be an enum type.");
        }
    }

    public class MappedHidDevice<UsageEnum>
        : MappedHidDevice
        where UsageEnum : struct
    {
        public override HidLayout layout => genericLayout;
        public HidLayout<UsageEnum> genericLayout => (HidLayout<UsageEnum>)parent.layoutMgr.GetLayout(productIdentifier);

        public MappedHidDevice(InputContext parent)
            : base(parent, typeof(UsageEnum))
        { }

        public InputAxis this[UsageEnum usage]
        {
            get
            {
                Tuple<uint, uint> hid = genericLayout.GetHid(usage);
                return this[hid.Item1][hid.Item2];
            }
        }

        public override string ToString() => $"{nameof(MappedHidDevice)}<{typeof(UsageEnum).Name}> {{ {genericLayout.ToString()} }}";
    }
}
*/
