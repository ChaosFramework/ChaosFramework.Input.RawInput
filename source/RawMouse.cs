using ChaosUtil.Platform.Windows.WinAPI.winuser;
using System.Windows.Forms;

namespace ChaosFramework.Input.RawInput
{
    using Collections.Immutable;

    public class RawMouse : Mouse
    {
        /// <summary> See <see cref="RAWMOUSE.RI_MOUSE"/> as to why. </summary>
        const int NUM_BUTTONS = 5;

        public new class Position(InputDevice parent, Direction direction)
            : Mouse.Position(parent, direction)
        {
            long exactNext, exactCurrent;

            public long exactValue => exactCurrent;
            public override float value => exactCurrent;

            protected override void AdvanceFrame()
            {
                exactCurrent = exactNext;
                base.AdvanceFrame();
            }

            internal void ProcessRaw(int rawDelta)
                => SetValue<Position>(exactNext += rawDelta);

            public override string GetAxisString() => direction.ToString();
        }

        public new class Button
            : Mouse.Button
        {
            readonly RAWMOUSE.RI_MOUSE downFlag, upFlag;

            public Button(Mouse parent, ButtonSemantic button)
                : base(parent, button)
            {
                downFlag = (RAWMOUSE.RI_MOUSE)(1 << ((int)button * 2));
                upFlag = (RAWMOUSE.RI_MOUSE)(2 << ((int)button * 2));
            }

            internal void ProcessRaw(RAWMOUSE.RI_MOUSE rawButtonFlags)
            {
                if ((rawButtonFlags & downFlag) != 0)
                    SetDown<Button>(true);
                if ((rawButtonFlags & upFlag) != 0)
                    SetDown<Button>(false);
            }
        }

        public new class Wheel(Mouse parent, WheelDirection dir)
            : Mouse.Wheel(parent, dir)
        {
            float abs;

            internal void Increment(float delta)
                => SetValue<Wheel>(abs += delta);
        }

        internal class RawDeviceImplementation(RawMouse parent)
            : RawDevice(parent)
        {
            readonly RawMouse mouse = parent;

            internal override void Init(RID_DEVICE_INFO info)
            {
                // TODO: figure out what this was for and if it's useful, delete otherwise
                uint mouseUsage = (uint)(info.hid.usUsagePage << 16) | info.hid.usUsage;
            }

            internal override void ProcessRaw(RAWINPUT raw, Message message, System.IntPtr buffer)
            {
                if (raw.mouse.usFlags == RAWMOUSE.MOUSE.MOVE_RELATIVE)
                {
                    mouse.x.ProcessRaw(raw.mouse.lLastX);
                    mouse.y.ProcessRaw(raw.mouse.lLastY);
                }

                if (raw.mouse.usButtonFlags == RAWMOUSE.RI_MOUSE.WHEEL)
                   mouse.scroll.Increment((float)raw.mouse.usButtonData / 120);

                if (raw.mouse.usButtonFlags == RAWMOUSE.RI_MOUSE.HWHEEL)
                   mouse.tilt.Increment((float)raw.mouse.usButtonData / 120);

                for (int i = 0; i < NUM_BUTTONS; i++)
                    mouse.buttons[i].ProcessRaw(raw.mouse.usButtonFlags);
            }
        }

        internal readonly RawDeviceImplementation implementation;

        internal Position x, y;
        internal Button[] buttons;
        internal Wheel scroll, tilt;

        public RawMouse(InputContext parent)
            : base(parent)
        {
            implementation = new RawDeviceImplementation(this);
        }

        protected override Mouse.Position GenerateAxis(Direction direction)
            => direction == Direction.X ? x = new Position(this, Direction.X) : y = new Position(this, Direction.Y);

        protected override ImmutableArray<Mouse.Button> GenerateButtons()
        {

            buttons = new Button[NUM_BUTTONS];
            for (int i = 0; i < NUM_BUTTONS; i++)
                buttons[i] = new Button(this, (ButtonSemantic)i);

            return buttons;
        }

        public override bool IsConnected()
            => true;

        protected override Mouse.Wheel GenerateWheel(WheelDirection dir)
            => dir == WheelDirection.Scroll
                ? scroll = new Wheel(this, dir)
                : tilt = new Wheel(this, dir);
    }
}
