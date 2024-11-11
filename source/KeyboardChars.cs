using ChaosUtil.Reflection;
using System.Xml;
using Key = ChaosFramework.Input.Windows.Keyboard.Keys;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Input.Windows
{
    using CharacterMap = SysCol.Dictionary<KeyboardLayouts, SysCol.Dictionary<KeyboardChars.Modifier, SysCol.Dictionary<Key, char>>>;
    using KeyMap = SysCol.Dictionary<KeyboardLayouts, SysCol.Dictionary<KeyboardChars.Modifier, Key[]>>;

    public static class KeyboardChars
    {
        [System.Flags]
        public enum Modifier : byte
        {
            None = 0,
            Shift = 1,
            Alt = 2,
            AltGr = 4,
            Ctrl = 8,
            All = Shift | Alt | AltGr | Ctrl
        }

        const KeyboardLayouts DEFAULT_LAYOUT = KeyboardLayouts.QWERTZ_GER;
        static readonly CharacterMap chars = new CharacterMap();

        static KeyboardChars()
        {
            using (System.IO.MemoryStream str = new System.IO.MemoryStream(Properties.Resources.KeyboardLayouts))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(str);
                foreach (XmlNode node in doc.SelectNodes("//Key"))
                {
                    XmlNode node_modifier = node.ParentNode;
                    XmlNode node_layout = node_modifier.ParentNode;
                    KeyboardLayouts laoyut = Enum<KeyboardLayouts>.Parse(node_layout.Attributes["id"].Value);
                    Modifier modifier = Enum<Modifier>.ParseFlags(node_modifier.Attributes["id"].Value);
                    Key key = Enum<Key>.Parse(node.Attributes["key"].Value);
                    char c = node.Attributes["char"].Value[0];

                    if (!chars.ContainsKey(laoyut))
                        chars[laoyut] = new SysCol.Dictionary<Modifier, SysCol.Dictionary<Key, char>>();

                    if (!chars[laoyut].ContainsKey(modifier))
                        chars[laoyut][modifier] = new SysCol.Dictionary<Key, char>();

                    chars[laoyut][modifier][key] = c;
                }
            }

            SysCol.Dictionary<Key, char> modMap;
            SysCol.Dictionary<Modifier, SysCol.Dictionary<Key, char>> layoutMap;
            foreach (KeyboardLayouts l in Enum<KeyboardLayouts>.GetValues())
                if (l != DEFAULT_LAYOUT)
                    foreach (SysCol.KeyValuePair<Modifier, SysCol.Dictionary<Key, char>> mod in chars[DEFAULT_LAYOUT])
                        foreach (SysCol.KeyValuePair<Key, char> key in chars[DEFAULT_LAYOUT][mod.Key])
                        {
                            if (!chars.TryGetValue(l, out layoutMap))
                                chars[l] = layoutMap = new SysCol.Dictionary<Modifier, SysCol.Dictionary<Key, char>>();

                            if (!layoutMap.TryGetValue(mod.Key, out modMap))
                                layoutMap[mod.Key] = modMap = new SysCol.Dictionary<Key, char>();

                            if (!modMap.ContainsKey(key.Key))
                                modMap[key.Key] = key.Value;
                        }
        }

        public static char GetChar(KeyboardLayouts layout, Modifier modifier, Key key)
        {
            SysCol.Dictionary<Modifier, SysCol.Dictionary<Key, char>> modifiers;
            SysCol.Dictionary<Key, char> keys;
            char outKey;
            if (!chars.TryGetValue(layout, out modifiers)) return '\0';
            if (!modifiers.TryGetValue(modifier, out keys)) return '\0';
            if (!keys.TryGetValue(key, out outKey)) return '\0';
            return outKey;
        }

        public static Modifier GetCharModifier(Keyboard keyboard)
        {
            const float THRESHOLD = 0.5f;
            Modifier mod = Modifier.None;
            if (keyboard[(int)Key.LeftShift].value > THRESHOLD || keyboard[(int)Key.RightShift].value > THRESHOLD) mod |= Modifier.Shift;
            if (keyboard[(int)Key.LeftControl].value > THRESHOLD || keyboard[(int)Key.RightControl].value > THRESHOLD) mod |= Modifier.Ctrl;
            if (keyboard[(int)Key.LeftAlt].value > THRESHOLD) mod |= Modifier.Alt;
            if (keyboard[(int)Key.AltGr].value > THRESHOLD) mod |= Modifier.AltGr;
            return mod;
        }

        public static Key[] GetDefinedKeys(KeyboardLayouts layout, Modifier modifier)
        {
            Collections.LinkedList<Key> lst = new Collections.LinkedList<Key>();
            foreach (SysCol.KeyValuePair<Key, char> c in chars[layout][modifier])
                lst.Add(c.Key);

            return lst.ToArray();
        }

        public static SysCol.Dictionary<Modifier, Key[]> GetDefinedKeys(KeyboardLayouts layout)
        {
            SysCol.Dictionary<Modifier, Key[]> keys = new SysCol.Dictionary<Modifier, Key[]>();
            for (Modifier mod = Modifier.None; mod <= Modifier.All; mod++)
                keys[mod] = GetDefinedKeys(layout, mod);

            return keys;
        }

        public static KeyMap GetDefinedKeys()
        {
            KeyMap keys = new KeyMap();
            foreach (KeyboardLayouts layout in Enum<KeyboardLayouts>.GetValues())
                keys[layout] = GetDefinedKeys(layout);

            return keys;
        }
    }
}
