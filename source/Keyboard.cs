using ChaosFramework.Collections;
using ChaosUtil.Platform.Windows.WinAPI.winuser;
using System;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Input.Windows
{
    using InputEvents;

    public class Keyboard : RawDevice
    {
        public enum Keys : ushort
        {
            FlagDown = RAWKEYBOARD.RI_KEY.MAKE << 8,
            FlagUp = RAWKEYBOARD.RI_KEY.BREAK << 8,
            FlagLeftKey = RAWKEYBOARD.RI_KEY.E0 << 8,
            FlagRightKey = RAWKEYBOARD.RI_KEY.E1 << 8,

            Null = 0 | FlagDown,
            Escape = 1 | FlagDown,
            D1 = 2 | FlagDown,
            D2 = 3 | FlagDown,
            D3 = 4 | FlagDown,
            D4 = 5 | FlagDown,
            D5 = 6 | FlagDown,
            D6 = 7 | FlagDown,
            D7 = 8 | FlagDown,
            D8 = 9 | FlagDown,
            D9 = 10 | FlagDown,
            D0 = 11 | FlagDown,
            Minus = 12 | FlagDown,
            Equals = 13 | FlagDown,
            BackSpace = 14 | FlagDown,
            Tab = 15 | FlagDown,
            Q = 16 | FlagDown,
            W = 17 | FlagDown,
            E = 18 | FlagDown,
            R = 19 | FlagDown,
            T = 20 | FlagDown,
            Y = 21 | FlagDown,
            U = 22 | FlagDown,
            I = 23 | FlagDown,
            O = 24 | FlagDown,
            P = 25 | FlagDown,
            LeftBracket = 26 | FlagDown,
            RightBracket = 27 | FlagDown,
            Return = 28 | FlagDown,
            NumPadReturn = 28 | FlagLeftKey,
            LeftControl = 29 | FlagDown,
            RightControl = 29 | FlagLeftKey,
            Pause = 29 | FlagRightKey,
            A = 30 | FlagDown,
            S = 31 | FlagDown,
            D = 32 | FlagDown,
            F = 33 | FlagDown,
            G = 34 | FlagDown,
            H = 35 | FlagDown,
            J = 36 | FlagDown,
            K = 37 | FlagDown,
            L = 38 | FlagDown,
            SemiColon = 39 | FlagDown,
            Apostrophe = 40 | FlagDown,
            Grave = 41 | FlagDown,
            LeftShift = 42 | FlagDown,
            BackSlash = 43 | FlagDown,
            Z = 44 | FlagDown,
            X = 45 | FlagDown,
            C = 46 | FlagDown,
            V = 47 | FlagDown,
            B = 48 | FlagDown,
            N = 49 | FlagDown,
            M = 50 | FlagDown,
            Comma = 51 | FlagDown,
            Period = 52 | FlagDown,
            Slash = 53 | FlagDown,
            NumPadDivide = 53 | FlagLeftKey,
            RightShift = 54 | FlagDown,
            NumPadMultiply = 55 | FlagDown,
            Print = 55 | FlagLeftKey,
            LeftAlt = 56 | FlagDown,
            AltGr = 56 | FlagLeftKey,
            Space = 57 | FlagDown,
            CapsLock = 58 | FlagDown,
            F1 = 59 | FlagDown,
            F2 = 60 | FlagDown,
            F3 = 61 | FlagDown,
            F4 = 62 | FlagDown,
            F5 = 63 | FlagDown,
            F6 = 64 | FlagDown,
            F7 = 65 | FlagDown,
            F8 = 66 | FlagDown,
            F9 = 67 | FlagDown,
            F10 = 68 | FlagDown,
            Numlock = 69 | FlagDown,
            Scroll = 70 | FlagDown,
            NumPad7 = 71 | FlagDown,
            Home = 71 | FlagLeftKey,
            NumPad8 = 72 | FlagDown,
            ArrowUp = 72 | FlagLeftKey,
            NumPad9 = 73 | FlagDown,
            PageUp = 73 | FlagLeftKey,
            NumPadMinus = 74 | FlagDown,
            NumPad4 = 75 | FlagDown,
            ArrowLeft = 75 | FlagLeftKey,
            NumPad5 = 76 | FlagDown,
            NumPad6 = 77 | FlagDown,
            ArrowRight = 77 | FlagLeftKey,
            NumPadPlus = 78 | FlagDown,
            NumPad1 = 79 | FlagDown,
            End = 79 | FlagLeftKey,
            NumPad2 = 80 | FlagDown,
            ArrowDown = 80 | FlagLeftKey,
            NumPad3 = 81 | FlagDown,
            PageDown = 81 | FlagLeftKey,
            NumPad0 = 82 | FlagDown,
            Insert = 82 | FlagLeftKey,
            NumPadPeriod = 83 | FlagDown,
            Delete = 83 | FlagLeftKey,
            OEM102 = 86 | FlagDown,
            LessThan = OEM102,
            VerticalBar = OEM102,
            F11 = 87 | FlagDown,
            F12 = 88 | FlagDown,
            LWindowsKey = 91 | FlagLeftKey,
            RWindowsKey = 92 | FlagLeftKey,
            Context = 93 | FlagLeftKey
        }

        public class Key : InputAxis
        {
            const float FIRST_REPEAT = 1;
            const float REPEAT_INTERVAL = 0.1f;

            public readonly Keys key;

            internal int nextValue;
            readonly System.Diagnostics.Stopwatch downTime;
            int repetitionsPerformed = 0;

            public Key(Keyboard parent, Keys key)
                : base(parent)
            {
                this.key = key;
                downTime = new System.Diagnostics.Stopwatch();
            }

            protected override void Update(object data)
            {
                if (nextValue >= pushThreshold && value < pushThreshold)
                {
                    repetitionsPerformed = 0;
                    downTime.Restart();
                }
                else if (nextValue < pushThreshold && value >= pushThreshold)
                {
                    repetitionsPerformed = int.MaxValue;
                    downTime.Stop();
                }

                value = nextValue;

                if (value >= pushThreshold)
                {
                    double timeSincePush = downTime.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency;
                    double timeSinceFirstPlannedRepeat = timeSincePush - FIRST_REPEAT;
                    if (timeSinceFirstPlannedRepeat > 0)
                    {
                        int numRepeatsThatShouldBePerformedByNow = (int)(timeSinceFirstPlannedRepeat / REPEAT_INTERVAL);
                        while (repetitionsPerformed < numRepeatsThatShouldBePerformedByNow)
                        {
                            repetitionsPerformed++;
                            AddEvent(new InputRepeatEvent<Key>(this, oldValue, value, repetitionsPerformed));
                        }
                    }
                }
            }

            public override string GetAxisString() => key.ToString();
        }

        public static bool WasActivated(InputContext input, Keys usage, float threshold = 0.5f)
            => input.EnumerateDevices<Keyboard>().Any(new Tuple<Keys, float>(usage, threshold), WasKeyActivated);

        static bool WasKeyActivated(Keyboard keyboard, Tuple<Keys, float> keyAndThreshold)
            => keyboard[keyAndThreshold.Item1].WasActivated(keyAndThreshold.Item2);

        public static bool WasReleased(InputContext input, Keys usage, float threshold = 0.5f)
            => input.EnumerateDevices<Keyboard>().Any(new Tuple<Keys, float>(usage, threshold), WasKeyReleased);

        static bool WasKeyReleased(Keyboard keyboard, Tuple<Keys, float> keyAndThreshold)
            => keyboard[keyAndThreshold.Item1].WasActivated(keyAndThreshold.Item2);

        public static float GetValue(InputContext input, Keys usage)
        {
            float value = 0;
            foreach (Keyboard keyboard in input.EnumerateDevices<Keyboard>())
                value = Math.Max(value, keyboard[usage].value);

            return value;
        }

        SysCol.Dictionary<Keys, Key> pressed = new SysCol.Dictionary<Keys, Key>();

        public InputAxis this[Keys key] => this[(uint)key].first;

        public Keyboard(InputContext parent)
            : base(parent)
        { }

        internal override void Init(RID_DEVICE_INFO info)
        {
            SysCol.HashSet<Keys> strictSet = new SysCol.HashSet<Keys>();
            foreach (Keys k in ChaosUtil.Reflection.Enum<Keys>.GetValues())
                strictSet.Add(k);

            foreach (Keys keyCode in strictSet)
            {
                Key key = new Key(this, keyCode);
                AddAxis((uint)keyCode, key);
                pressed[keyCode] = key;
            }
        }

        internal override void ProcessRaw(RAWINPUT raw, System.Windows.Forms.Message message, IntPtr buffer)
        {
            if (raw.header.dwType == RIM_TYPE.KEYBOARD)
            {
                int rawKey = raw.keyboard.VKey;
                if (rawKey >= 0xFF)
                    return;

                string rawKeyName = Enum.GetName(typeof(System.Windows.Forms.Keys), rawKey);
                Keys myKey = (Keys)(raw.keyboard.MakeCode | ((ushort)(raw.keyboard.Flags & ~RAWKEYBOARD.RI_KEY.BREAK) << 8));

                Key key;
                if (!pressed.TryGetValue(myKey, out key))
                    pressed[myKey] = key = new Key(this, myKey);

                int oldValue = key.nextValue;
                WM wm = (WM)raw.keyboard.Message;
                if ((wm == WM.KEYDOWN || wm == WM.SYSKEYDOWN) && oldValue != 1)
                    AddEvent(new InputPushEvent<Key>(key, oldValue, key.nextValue = 1));
                else if ((wm == WM.KEYUP || wm == WM.SYSKEYUP) && oldValue != 0)
                    AddEvent(new InputReleaseEvent<Key>(key, oldValue, key.nextValue = 0));
            }
        }
    }
}
