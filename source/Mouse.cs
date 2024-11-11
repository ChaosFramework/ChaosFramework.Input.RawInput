using ChaosFramework.Collections;
using ChaosUtil.Platform.Windows.WinAPI.winuser;
using System;
using System.Linq;
using System.Windows.Forms;

namespace ChaosFramework.Input.Windows
{
    using InputEvents;

    public class Mouse : RawDevice
    {
        public enum MouseParameters
        {
            XPositive = 0,
            XNegative = 1,
            YPositive = 2,
            YNegative = 3,
            ZPositive = 4,
            ZNegative = 5,
            Button = 6,
            LeftButton = 6,
            RightButton = 7,
            Wheel = 8,
        }

        public class Axis : InputAxis
        {
            public readonly MouseParameters usage;

            internal float internalValue;

            internal Axis(InputDevice parent, MouseParameters usage)
                : base(parent)
            {
                this.usage = usage;
            }

            protected override void Update(object data)
            {
                value = internalValue;
                if (usage < MouseParameters.Button)
                    internalValue = 0;
            }

            public override string GetAxisString() => usage.ToString();

            public override float ValueExponential(float exponent = 4) => value;
        }

        public static bool WasActivated(InputContext input, MouseParameters usage, float threshold = 0.5f)
            => input.EnumerateDevices<Mouse>().Any(new Tuple<MouseParameters, float>(usage, threshold), WasButtonActivated);

        static bool WasButtonActivated(Mouse mouse, Tuple<MouseParameters, float> mouseParamAndThreshold)
            => mouse[mouseParamAndThreshold.Item1].WasActivated(mouseParamAndThreshold.Item2);

        public static bool WasReleased(InputContext input, MouseParameters usage, float threshold = 0.5f)
            => input.EnumerateDevices<Mouse>().Any(new Tuple<MouseParameters, float>(usage, threshold), WasButtonReleased);

        static bool WasButtonReleased(Mouse mouse, Tuple<MouseParameters, float> mouseParamAndThreshold)
            => mouse[mouseParamAndThreshold.Item1].WasReleased(mouseParamAndThreshold.Item2);

        private static float SelectX(Mouse m) => m.deltaX;
        private static float SelectY(Mouse m) => m.deltaY;
        private static float SelectWheel(Mouse m) => m.deltaZ;

        public static float GetPositionDeltaX(InputContext input)
            => input.EnumerateDevices<Mouse>().Sum((Func<Mouse, float>)SelectX);

        public static float GetPositionDeltaY(InputContext input)
            => input.EnumerateDevices<Mouse>().Sum((Func<Mouse, float>)SelectY);

        public static float GetWheelDelta(InputContext input)
            => input.EnumerateDevices<Mouse>().Sum((Func<Mouse, float>)SelectWheel);

        public static float GetValue(InputContext input, MouseParameters usage)
        {
            float value = 0;
            foreach (Mouse mouse in input.EnumerateDevices<Mouse>())
                value = Math.Max(value, mouse[usage].value);

            return value;
        }

        float deltaX, deltaY, deltaZ;
        Axis[] internalAxis = new Axis[0];

        public Mouse(InputContext parent)
            : base(parent)
        { }

        public Axis this[MouseParameters usage]
        {
            get
            {
                int usageInd = (int)usage;
                if (usageInd >= internalAxis.Length)
                    return null;

                return internalAxis[usageInd];
            }
        }

        internal override void Init(RID_DEVICE_INFO info)
        {
            uint mouseUsage = (uint)(info.hid.usUsagePage << 16) | info.hid.usUsage;
            LinkedList<Axis> axes = new LinkedList<Axis>();
            for (int i = 0; i < (int)MouseParameters.Button; i++)
                axes.Add(new Axis(this, (MouseParameters)i));

            for (int i = 0; i < 5; i++)
                axes.Add(new Axis(this, MouseParameters.Button + i));

            AddAxis(mouseUsage, internalAxis = axes.ToArray());
        }

        internal override void ProcessRaw(RAWINPUT raw, Message message, IntPtr buffer)
        {
            if (raw.mouse.usFlags == RAWMOUSE.MOUSE.MOVE_RELATIVE)
            {
                int dx = raw.mouse.lLastX;
                int dy = raw.mouse.lLastY;
                internalAxis[(int)MouseParameters.XNegative].internalValue += -dx;
                internalAxis[(int)MouseParameters.XPositive].internalValue += dx;
                internalAxis[(int)MouseParameters.YNegative].internalValue += -dy;
                internalAxis[(int)MouseParameters.YPositive].internalValue += dy;
                deltaX += dx;
                deltaY += dy;

                if (deltaX != 0) AddEvent(new InputChangeEvent<Axis>(internalAxis[(int)MouseParameters.XPositive], 0, deltaX));
                if (deltaY != 0) AddEvent(new InputChangeEvent<Axis>(internalAxis[(int)MouseParameters.YPositive], 0, deltaY));
            }

            if (raw.mouse.usButtonFlags == RAWMOUSE.RI_MOUSE.WHEEL)
            {
                float dz = (float)raw.mouse.usButtonData / 120;
                deltaZ += dz;

                if (raw.mouse.usButtonData < 0)
                    internalAxis[(int)MouseParameters.ZNegative].internalValue -= dz;
                else
                    internalAxis[(int)MouseParameters.ZPositive].internalValue += dz;

                AddEvent(new InputChangeEvent<Axis>(internalAxis[(int)MouseParameters.ZPositive], 0, deltaZ));
            }

            for (int i = 0; i < 5; i++)
            {
                Axis axis = internalAxis[(int)MouseParameters.Button + i];
                if (((int)raw.mouse.usButtonFlags & (1 << (i * 2))) != 0)
                    AddEvent(new InputPushEvent<Axis>(axis, axis.internalValue, axis.internalValue = 1));
                else if (((int)raw.mouse.usButtonFlags & (2 << (i * 2))) != 0)
                    AddEvent(new InputReleaseEvent<Axis>(axis, axis.internalValue, axis.internalValue = 0));
            }
        }

        public override void Update(bool noop = false)
        {
            base.Update(noop);
            deltaX = deltaY = deltaZ = 0;
        }

        public float GetPositionDeltaX() => deltaX;
        public float GetPositionDeltaY() => deltaY;
        public float GetWheelDelta() => deltaZ;
    }
}
