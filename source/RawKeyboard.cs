using ChaosUtil.Platform.Windows.WinAPI.winuser;
using System;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Input.RawInput
{
    using InputEvents;

    public class RawKeyboard : Keyboard
    {
        internal enum RawKeys : ushort
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
            Equal = 13 | FlagDown,
            Backspace = 14 | FlagDown,
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
            KeypadEnter = 28 | FlagLeftKey,
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
            Semicolon = 39 | FlagDown,
            Quote = 40 | FlagDown,
            Grave = 41 | FlagDown,
            LeftShift = 42 | FlagDown,
            Backslash = 43 | FlagDown,
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
            KeypadDivide = 53 | FlagLeftKey,
            RightShift = 54 | FlagDown,
            KeypadMultiply = 55 | FlagDown,
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
            Keypad7 = 71 | FlagDown,
            Home = 71 | FlagLeftKey,
            Keypad8 = 72 | FlagDown,
            ArrowUp = 72 | FlagLeftKey,
            Keypad9 = 73 | FlagDown,
            PageUp = 73 | FlagLeftKey,
            KeypadMinus = 74 | FlagDown,
            Keypad4 = 75 | FlagDown,
            ArrowLeft = 75 | FlagLeftKey,
            Keypad5 = 76 | FlagDown,
            Keypad6 = 77 | FlagDown,
            ArrowRight = 77 | FlagLeftKey,
            KeypadPlus = 78 | FlagDown,
            Keypad1 = 79 | FlagDown,
            End = 79 | FlagLeftKey,
            Keypad2 = 80 | FlagDown,
            ArrowDown = 80 | FlagLeftKey,
            Keypad3 = 81 | FlagDown,
            PageDown = 81 | FlagLeftKey,
            Keypad0 = 82 | FlagDown,
            Insert = 82 | FlagLeftKey,
            KeypadPeriod = 83 | FlagDown,
            Delete = 83 | FlagLeftKey,
            NonUsBackslashAndPipe = 86 | FlagDown,
            LessThan = NonUsBackslashAndPipe,
            VerticalBar = NonUsBackslashAndPipe,
            F11 = 87 | FlagDown,
            F12 = 88 | FlagDown,
            LWindowsKey = 91 | FlagLeftKey,
            RWindowsKey = 92 | FlagLeftKey,
            Context = 93 | FlagLeftKey
        }

        readonly static SysCol.Dictionary<HidUsage, RawKeys> hidUsage2RawKey = new SysCol.Dictionary<HidUsage, RawKeys>()
        {
            [HidUsage.Escape] = RawKeys.Escape,
            [HidUsage.D1] = RawKeys.D1,
            [HidUsage.D2] = RawKeys.D2,
            [HidUsage.D3] = RawKeys.D3,
            [HidUsage.D4] = RawKeys.D4,
            [HidUsage.D5] = RawKeys.D5,
            [HidUsage.D6] = RawKeys.D6,
            [HidUsage.D7] = RawKeys.D7,
            [HidUsage.D8] = RawKeys.D8,
            [HidUsage.D9] = RawKeys.D9,
            [HidUsage.D0] = RawKeys.D0,
            [HidUsage.Minus] = RawKeys.Minus,
            [HidUsage.Equal] = RawKeys.Equal,
            [HidUsage.Backspace] = RawKeys.Backspace,
            [HidUsage.Tab] = RawKeys.Tab,
            [HidUsage.Q] = RawKeys.Q,
            [HidUsage.W] = RawKeys.W,
            [HidUsage.E] = RawKeys.E,
            [HidUsage.R] = RawKeys.R,
            [HidUsage.T] = RawKeys.T,
            [HidUsage.Y] = RawKeys.Y,
            [HidUsage.U] = RawKeys.U,
            [HidUsage.I] = RawKeys.I,
            [HidUsage.O] = RawKeys.O,
            [HidUsage.P] = RawKeys.P,
            [HidUsage.LeftBracket] = RawKeys.LeftBracket,
            [HidUsage.RightBracket] = RawKeys.RightBracket,
            [HidUsage.Return] = RawKeys.Return,
            [HidUsage.KeypadEnter] = RawKeys.KeypadEnter,
            [HidUsage.ControlLeft] = RawKeys.LeftControl,
            [HidUsage.ControlRight] = RawKeys.RightControl,
            [HidUsage.Pause] = RawKeys.Pause,
            [HidUsage.A] = RawKeys.A,
            [HidUsage.S] = RawKeys.S,
            [HidUsage.D] = RawKeys.D,
            [HidUsage.F] = RawKeys.F,
            [HidUsage.G] = RawKeys.G,
            [HidUsage.H] = RawKeys.H,
            [HidUsage.J] = RawKeys.J,
            [HidUsage.K] = RawKeys.K,
            [HidUsage.L] = RawKeys.L,
            [HidUsage.Semicolon] = RawKeys.Semicolon,
            [HidUsage.Quote] = RawKeys.Quote,
            [HidUsage.Grave] = RawKeys.Grave,
            [HidUsage.ShiftLeft] = RawKeys.LeftShift,
            [HidUsage.Backslash] = RawKeys.Backslash,
            [HidUsage.Z] = RawKeys.Z,
            [HidUsage.X] = RawKeys.X,
            [HidUsage.C] = RawKeys.C,
            [HidUsage.V] = RawKeys.V,
            [HidUsage.B] = RawKeys.B,
            [HidUsage.N] = RawKeys.N,
            [HidUsage.M] = RawKeys.M,
            [HidUsage.Comma] = RawKeys.Comma,
            [HidUsage.Period] = RawKeys.Period,
            [HidUsage.Slash] = RawKeys.Slash,
            [HidUsage.KeypadDivide] = RawKeys.KeypadDivide,
            [HidUsage.ShiftRight] = RawKeys.RightShift,
            [HidUsage.KeypadMultiply] = RawKeys.KeypadMultiply,
            [HidUsage.PrintScreen] = RawKeys.Print,
            [HidUsage.AltLeft] = RawKeys.LeftAlt,
            [HidUsage.AltRight] = RawKeys.AltGr,
            [HidUsage.Space] = RawKeys.Space,
            [HidUsage.CapsLock] = RawKeys.CapsLock,
            [HidUsage.F1] = RawKeys.F1,
            [HidUsage.F2] = RawKeys.F2,
            [HidUsage.F3] = RawKeys.F3,
            [HidUsage.F4] = RawKeys.F4,
            [HidUsage.F5] = RawKeys.F5,
            [HidUsage.F6] = RawKeys.F6,
            [HidUsage.F7] = RawKeys.F7,
            [HidUsage.F8] = RawKeys.F8,
            [HidUsage.F9] = RawKeys.F9,
            [HidUsage.F10] = RawKeys.F10,
            [HidUsage.NumLock] = RawKeys.Numlock,
            [HidUsage.ScrollLock] = RawKeys.Scroll,
            [HidUsage.Keypad7] = RawKeys.Keypad7,
            [HidUsage.Home] = RawKeys.Home,
            [HidUsage.Keypad8] = RawKeys.Keypad8,
            [HidUsage.ArrowUp] = RawKeys.ArrowUp,
            [HidUsage.Keypad9] = RawKeys.Keypad9,
            [HidUsage.PageUp] = RawKeys.PageUp,
            [HidUsage.KeypadMinus] = RawKeys.KeypadMinus,
            [HidUsage.Keypad4] = RawKeys.Keypad4,
            [HidUsage.ArrowLeft] = RawKeys.ArrowLeft,
            [HidUsage.Keypad5] = RawKeys.Keypad5,
            [HidUsage.Keypad6] = RawKeys.Keypad6,
            [HidUsage.ArrowRight] = RawKeys.ArrowRight,
            [HidUsage.KeypadPlus] = RawKeys.KeypadPlus,
            [HidUsage.Keypad1] = RawKeys.Keypad1,
            [HidUsage.End] = RawKeys.End,
            [HidUsage.Keypad2] = RawKeys.Keypad2,
            [HidUsage.ArrowDown] = RawKeys.ArrowDown,
            [HidUsage.Keypad3] = RawKeys.Keypad3,
            [HidUsage.PageDown] = RawKeys.PageDown,
            [HidUsage.Keypad0] = RawKeys.Keypad0,
            [HidUsage.Insert] = RawKeys.Insert,
            [HidUsage.KeypadPeriod] = RawKeys.KeypadPeriod,
            [HidUsage.Delete] = RawKeys.Delete,
            [HidUsage.NonUsBackslashAndPipe] = RawKeys.NonUsBackslashAndPipe,
            // TODO: [HidUsage.Unknown] = RawKeys.LessThan,
            // TODO: [HidUsage.Unknown] = RawKeys.VerticalBar,
            [HidUsage.F11] = RawKeys.F11,
            [HidUsage.F12] = RawKeys.F12,
            [HidUsage.Application] = RawKeys.LWindowsKey,
            // TODO: [HidUsage.Unknown] = RawKeys.RWindowsKey,
            [HidUsage.Menu] = RawKeys.Context,
        };

        public class RawKey : Key
        {
            const float FIRST_REPEAT = 1;
            const float REPEAT_INTERVAL = 0.1f;

            internal readonly RawKeys rawKey;

            public RawKey(RawKeyboard parent, HidUsage usage)
                : base(parent, usage)
            {
                if (!hidUsage2RawKey.TryGetValue(usage, out rawKey))
                    rawKey = RawKeys.Null;
            }

            internal void ProcessRaw(WM message)
            {
                // TODO: MAYBE pressing shift on another keyboard while holding down a key repeatedly triggers WM.SYSKEYDOWN or something
                if (message == WM.KEYDOWN || message == WM.SYSKEYDOWN)
                    SetDown<RawKey>(true);
                else if (message == WM.KEYUP || message == WM.SYSKEYUP)
                    SetDown<RawKey>(false);
            }
        }

        internal class RawDeviceImplementation(RawKeyboard parent) : RawDevice(parent)
        {
            RawKeyboard keyboard = parent;

            internal override void Init(RID_DEVICE_INFO info)
            { }

            internal override void ProcessRaw(RAWINPUT raw, System.Windows.Forms.Message message, IntPtr buffer)
            {
                if (raw.header.dwType == RIM_TYPE.KEYBOARD)
                {
                    int rawKey = raw.keyboard.VKey;
                    if (rawKey >= 0xFF)
                        return;

                    string rawKeyName = Enum.GetName(typeof(System.Windows.Forms.Keys), rawKey);
                    RawKeys myKey = (RawKeys)(raw.keyboard.MakeCode | ((ushort)(raw.keyboard.Flags & ~RAWKEYBOARD.RI_KEY.BREAK) << 8));

                    RawKey key;
                    if (!keyboard.rawKey2AxisInstance.TryGetValue(myKey, out key))
                    {
                        System.Diagnostics.Debug.Fail($"Got a rawinput key that we don't know what do do with: {myKey}");
                        return;
                    }

                    key.ProcessRaw((WM)raw.keyboard.Message);
                }
            }
        }

        SysCol.Dictionary<RawKeys, RawKey> rawKey2AxisInstance = new SysCol.Dictionary<RawKeys, RawKey>();
        internal readonly RawDeviceImplementation implementation;

        public RawKeyboard(InputContext parent)
            : base(parent)
        {
            implementation = new RawDeviceImplementation(this);
        }

        protected override Key GenerateKey(HidUsage hidUsage)
        {
            RawKey axis = new RawKey(this, hidUsage);
            rawKey2AxisInstance[axis.rawKey] = axis;
            return axis;
        }

        public override bool IsConnected()
            => true;
    }
}
