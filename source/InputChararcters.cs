using ChaosUtil.Primitives;
using ChaosUtil.Reflection;
using System.Linq;
using Culture = System.Globalization.CultureInfo;
using NumberStyles = System.Globalization.NumberStyles;
using SysCol = System.Collections.Generic;
using Xml = System.Xml;

namespace ChaosFramework.Input.RawInput
{
    using Layouts;
    using KeyboardLayoutMap = SysCol.Dictionary<KeyboardLayouts, SysCol.Dictionary<Keyboard.Keys, InputChararcters.Axis>>;
    using MappedHidBaseLayoutMap = SysCol.Dictionary<System.Type, SysCol.Dictionary<object, InputChararcters.Axis>>;
    using MappedHidLayoutMap = SysCol.Dictionary<Layouts.HidLayout, SysCol.Dictionary<object, InputChararcters.Axis>>;
    using UnmappedHid = SysCol.Dictionary<uint, SysCol.Dictionary<uint, InputChararcters.Axis>>;

    public class InputChararcters
    {
        public class Axis
        {
            public readonly UnicodeChars character;
            public readonly string comment;
            public readonly object key;

            internal Axis(object key, UnicodeChars character, string comment = null)
            {
                this.key = key;
                this.character = character;
                this.comment = comment;
            }

            public static Axis Parse(System.Type keyType, string line)
            {
                string[] valueAndComment = line.Split(COMMENT_START, 2, System.StringSplitOptions.None);
                if (valueAndComment.Length < 1)
                    return null;

                string comment = null;
                if (valueAndComment.Length == 2)
                    comment = valueAndComment[1].Trim();

                string[] keyAndValue = valueAndComment[0].Split(VALUE_SPLIT, 2);
                if (keyAndValue.Length != 2)
                    return null;

                uint charIndex;
                if (!uint.TryParse(keyAndValue[1], NumberStyles.HexNumber, Culture.InvariantCulture, out charIndex))
                    return null;

                System.Delegate keyParser;
                if (!ChaosUtil.Serialization.Text.Parse.TryGetParser(keyType, out keyParser))
                    return null;

                object[] parserArgs = new[] { keyAndValue[0], System.Activator.CreateInstance(keyType) };
                if (!(bool)keyParser.DynamicInvoke(parserArgs))
                    return null;

                return new Axis(parserArgs[1], (UnicodeChars)charIndex, comment);
            }

            public static implicit operator UnicodeChars(Axis axis) => axis.character;
        }

        static readonly string[] COMMENT_START = new[] { "//" };
        static readonly char[] VALUE_SPLIT = new[] { '-' };
        static readonly char[] ASSIGNMENT = new[] { '=' };
        static readonly char[] LINE_SEPARATORS = new[] { '\n' };
        static readonly char[] BLANK = new[] { ' ', '\t' };

        readonly LayoutManager layoutMgr;

        UnicodeChars deviceNullChar = UnicodeChars.Null;
        UnicodeChars axisNullChar = UnicodeChars.Null;
        UnicodeChars unassignedChar = UnicodeChars.Null;

        UnicodeChars? unmappedHidIcon = null;
        readonly SysCol.Dictionary<System.Type, UnicodeChars> mappedHidBaseIcons = new SysCol.Dictionary<System.Type, UnicodeChars>();
        readonly SysCol.Dictionary<HidLayout, UnicodeChars> mappedHidIcons = new SysCol.Dictionary<HidLayout, UnicodeChars>();
        readonly SysCol.Dictionary<KeyboardLayouts, UnicodeChars> keyboardIcons = new SysCol.Dictionary<KeyboardLayouts, UnicodeChars>();

        readonly KeyboardLayoutMap keyboardLayouts = new KeyboardLayoutMap();
        readonly MappedHidBaseLayoutMap mappedHidBaseLayouts = new MappedHidBaseLayoutMap();
        readonly MappedHidLayoutMap mappedHidLayouts = new MappedHidLayoutMap();
        readonly UnmappedHid unmappedHid = new UnmappedHid();

        public InputChararcters(System.IO.Stream stream, LayoutManager layoutMgr)
        {
            this.layoutMgr = layoutMgr;

            Xml.XmlDocument xml = new Xml.XmlDocument();
            xml.Load(stream);

            Xml.XmlNode root = xml.SelectSingleNode("/Input");
            if (root != null)
            {
                Xml.XmlAttribute attrDeviceNullChar = root.Attributes["deviceNullChar"];
                if (attrDeviceNullChar != null)
                {
                    uint characterIndex;
                    if (uint.TryParse(attrDeviceNullChar.Value, NumberStyles.HexNumber, Culture.InvariantCulture, out characterIndex))
                        deviceNullChar = (UnicodeChars)characterIndex;
                }

                Xml.XmlAttribute attrAxisNullChar = root.Attributes["axisNullChar"];
                if (attrAxisNullChar != null)
                {
                    uint characterIndex;
                    if (uint.TryParse(attrAxisNullChar.Value, NumberStyles.HexNumber, Culture.InvariantCulture, out characterIndex))
                        axisNullChar = (UnicodeChars)characterIndex;
                }

                Xml.XmlAttribute attrUnassignedChar = root.Attributes["unassignedChar"];
                if (attrUnassignedChar != null)
                {
                    uint characterIndex;
                    if (uint.TryParse(attrUnassignedChar.Value, NumberStyles.HexNumber, Culture.InvariantCulture, out characterIndex))
                        unassignedChar = (UnicodeChars)characterIndex;
                }

                Xml.XmlNode keyboardNode = root.SelectSingleNode("Keyboard");
                if (keyboardNode != null)
                    LoadKeyboard(keyboardNode);

                Xml.XmlNode hidNode = root.SelectSingleNode("HID");
                if (hidNode != null)
                    LoadUnmappedHid(hidNode);

                Xml.XmlNode mappedHidNode = root.SelectSingleNode("MappedHID");
                if (mappedHidNode != null)
                    LoadMappedHid(mappedHidNode);
            }
        }

        void LoadKeyboard(Xml.XmlNode keyboardNode)
        {
            foreach (Xml.XmlNode layoutNode in keyboardNode.SelectNodes("Layout"))
            {
                Xml.XmlAttribute attrName = layoutNode.Attributes["name"];
                if (attrName != null)
                {
                    KeyboardLayouts layout;
                    if (System.Enum.TryParse(attrName.Value, out layout))
                    {
                        SysCol.Dictionary<Keyboard.Keys, Axis> dict;
                        if (!keyboardLayouts.TryGetValue(layout, out dict))
                            keyboardLayouts[layout] = dict = new SysCol.Dictionary<Keyboard.Keys, Axis>();

                        UnicodeChars? icon = ParseIconAttribute(layoutNode);
                        if (icon != null)
                            keyboardIcons[layout] = icon.Value;

                        Xml.XmlAttribute attrBase = layoutNode.Attributes["base"];
                        if (attrBase != null)
                        {
                            KeyboardLayouts baseLayout;
                            if (System.Enum.TryParse(attrBase.Value, out baseLayout))
                            {
                                SysCol.Dictionary<Keyboard.Keys, Axis> baseDict;
                                if (keyboardLayouts.TryGetValue(baseLayout, out baseDict))
                                    foreach (SysCol.KeyValuePair<Keyboard.Keys, Axis> entry in baseDict)
                                        dict[entry.Key] = entry.Value;

                                if (icon == null)
                                {
                                    UnicodeChars baseChar;
                                    if (keyboardIcons.TryGetValue(baseLayout, out baseChar))
                                        keyboardIcons[layout] = baseChar;
                                }
                            }
                        }

                        foreach (string line in layoutNode.InnerText.Split(LINE_SEPARATORS, System.StringSplitOptions.RemoveEmptyEntries))
                        {
                            Axis axis = Axis.Parse(typeof(Keyboard.Keys), line);
                            if (axis != null)
                                dict[(Keyboard.Keys)axis.key] = axis;
                        }
                    }
                }
            }
        }

        void LoadUnmappedHid(Xml.XmlNode unmappedHidNode)
        {
            UnicodeChars? icon = ParseIconAttribute(unmappedHidNode);
            if (icon != null)
                unmappedHidIcon = icon.Value;

            foreach (string line in unmappedHidNode.InnerText.Split(LINE_SEPARATORS, System.StringSplitOptions.RemoveEmptyEntries))
            {
                string[] assignmentAndComment = line.Split(COMMENT_START, 2, System.StringSplitOptions.None);
                if (assignmentAndComment.Length >= 1)
                {
                    string comment = string.Empty;
                    if (assignmentAndComment.Length == 2)
                        comment = assignmentAndComment[1].Trim();

                    string[] keyAndValue = assignmentAndComment[0].Split(ASSIGNMENT, 2, System.StringSplitOptions.None);
                    if (keyAndValue.Length == 2)
                    {
                        uint charIndex;
                        if (uint.TryParse(keyAndValue[1], NumberStyles.HexNumber, Culture.InvariantCulture, out charIndex))
                        {
                            string[] keyEntries = keyAndValue[0].Split(BLANK, 3, System.StringSplitOptions.RemoveEmptyEntries);
                            if (keyEntries.Length == 2 || keyEntries.Length == 3)
                            {
                                ushort page;
                                if (ushort.TryParse(keyEntries[0], NumberStyles.HexNumber, Culture.InvariantCulture, out page))
                                {
                                    ushort index;
                                    if (ushort.TryParse(keyEntries[1], NumberStyles.HexNumber, Culture.InvariantCulture, out index))
                                    {
                                        uint subIndex = 0;
                                        if (keyEntries.Length == 2
                                            || uint.TryParse(keyEntries[2], NumberStyles.HexNumber, Culture.InvariantCulture, out subIndex)
                                           )
                                        {
                                            uint hidKey = (uint)(page << 16 | index);
                                            ulong key = ((ulong)hidKey << 32) | subIndex;

                                            SysCol.Dictionary<uint, Axis> dict;
                                            if (!unmappedHid.TryGetValue(hidKey, out dict))
                                                unmappedHid[hidKey] = dict = new SysCol.Dictionary<uint, Axis>();

                                            dict[subIndex] = new Axis(key, (UnicodeChars)charIndex, comment);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        void LoadMappedHid(Xml.XmlNode mappedHidNode)
        {
            foreach (Xml.XmlNode layoutNode in mappedHidNode.SelectNodes("BaseLayout"))
            {
                Xml.XmlAttribute attrType = layoutNode.Attributes["type"];
                if (attrType != null)
                {
                    System.Type enumType;
                    if (AssemblyManager.TryGetTypeByFullName(LayoutManager.MAPPED_USAGES_NAMESPACE + attrType.Value, out enumType))
                    {
                        UnicodeChars? icon = ParseIconAttribute(layoutNode);
                        if (icon != null)
                            mappedHidBaseIcons[enumType] = icon.Value;

                        SysCol.Dictionary<object, Axis> dict;
                        if (!mappedHidBaseLayouts.TryGetValue(enumType, out dict))
                            mappedHidBaseLayouts[enumType] = dict = new SysCol.Dictionary<object, Axis>();

                        ParseHidLayoutBody(enumType, dict, layoutNode.InnerText);
                    }
                }
            }

            foreach (Xml.XmlNode layoutNode in mappedHidNode.SelectNodes("Layout"))
            {
                Xml.XmlAttribute attrName = layoutNode.Attributes["name"];
                if (attrName != null)
                {
                    HidLayout layout = layoutMgr.GetLayout(attrName.Value);
                    if (layout != null)
                    {
                        SysCol.Dictionary<object, Axis> dict;
                        if (!mappedHidLayouts.TryGetValue(layout, out dict))
                            mappedHidLayouts[layout] = dict = new SysCol.Dictionary<object, Axis>();

                        Xml.XmlAttribute attrBase = layoutNode.Attributes["base"];
                        if (attrBase != null)
                        {
                            HidLayout baseLayout = layoutMgr.GetLayout(attrBase.Value);
                            if (baseLayout != null)
                            {
                                SysCol.Dictionary<object, Axis> baseDict;
                                if (mappedHidLayouts.TryGetValue(baseLayout, out baseDict))
                                    foreach (SysCol.KeyValuePair<object, Axis> entry in baseDict)
                                        dict[entry.Key] = entry.Value;

                                UnicodeChars baseIcon;
                                if (mappedHidIcons.TryGetValue(layout, out baseIcon))
                                    mappedHidIcons[layout] = baseIcon;
                            }
                            else
                            {
                                System.Type baseType;
                                if (AssemblyManager.TryGetTypeByFullName(
                                    LayoutManager.MAPPED_USAGES_NAMESPACE + attrBase.Value,
                                    out baseType)
                                   )
                                {
                                    SysCol.Dictionary<object, Axis> baseDict;
                                    if (mappedHidBaseLayouts.TryGetValue(baseType, out baseDict))
                                        foreach (SysCol.KeyValuePair<object, Axis> entry in baseDict)
                                            dict[entry.Key] = entry.Value;

                                    UnicodeChars baseIcon;
                                    if (mappedHidBaseIcons.TryGetValue(baseType, out baseIcon))
                                        mappedHidIcons[layout] = baseIcon;
                                }
                            }
                        }

                        UnicodeChars? icon = ParseIconAttribute(layoutNode);
                        if (icon != null)
                            mappedHidIcons[layout] = icon.Value;

                        ParseHidLayoutBody(layout.enumType, dict, layoutNode.InnerText);
                    }
                }
            }
        }

        void ParseHidLayoutBody(System.Type enumType, SysCol.Dictionary<object, Axis> layout, string body)
        {
            foreach (Axis axis in
                from line in body.Split(LINE_SEPARATORS, System.StringSplitOptions.RemoveEmptyEntries)
                let axis = Axis.Parse(enumType, line)
                where axis != null
                select axis
                )
                layout[axis.key] = axis;
        }

        UnicodeChars? ParseIconAttribute(Xml.XmlNode node)
        {
            Xml.XmlAttribute attrIcon = node.Attributes["icon"];
            if (attrIcon != null)
            {
                uint characterIndex;
                if (uint.TryParse(attrIcon.Value, NumberStyles.HexNumber, Culture.InvariantCulture, out characterIndex))
                    return (UnicodeChars)characterIndex;
            }

            return null;
        }

        public UnicodeChars GetUnassignedChar() => unassignedChar;

        public UnicodeChars GetAxisNullChar() => axisNullChar;

        public UnicodeChars GetAxisChar(InputAxis axis, KeyboardLayouts keyboardLayout)
        {
            if (axis is Keyboard.Key)
                return GetAxisChar((Keyboard.Key)axis, keyboardLayout);
            else if (axis is Mouse.Axis)
                throw new System.NotSupportedException("No mouse layouts supported yet.");
            else if (axis is HidDevice.HidAxis)
                return GetAxisChar((HidDevice.HidAxis)axis);
            else
                return axisNullChar;
        }

        public UnicodeChars GetAxisChar(Keyboard.Key key, KeyboardLayouts keyboardLayout)
        {
            SysCol.Dictionary<Keyboard.Keys, Axis> layout;
            if (keyboardLayouts.TryGetValue(keyboardLayout, out layout))
            {
                Axis character;
                if (layout.TryGetValue(key.key, out character))
                    return character.character;
            }

            return axisNullChar;
        }

        public UnicodeChars GetAxisChar(HidDevice.HidAxis axis)
            => axis.parent is MappedHidDevice
               ? GetAxisChar((MappedHidDevice)axis.parent, axis)
               : GetUnmappedHidChar(axis);

        public UnicodeChars GetAxisChar(MappedHidDevice device, HidDevice.HidAxis axis)
        {
            int subIndex = axis.GetSubAxisIndex();
            if (subIndex >= 0)
            {
                SysCol.Dictionary<object, Axis> dict;
                if (mappedHidLayouts.TryGetValue(device.layout, out dict))
                {
                    object usage = device.layout.GetUsage(axis.parent.GetAxisIndex(axis), (uint)subIndex);
                    if (usage != null)
                    {
                        Axis layoutAxis;
                        if (dict.TryGetValue(usage, out layoutAxis))
                            return layoutAxis.character;
                    }
                }
            }

            return GetUnmappedHidChar(axis);
        }

        UnicodeChars GetUnmappedHidChar(HidDevice.HidAxis axis)
        {
            SysCol.Dictionary<uint, Axis> dict;
            if (!unmappedHid.TryGetValue(axis.parent.GetAxisIndex(axis), out dict))
                return axisNullChar;

            int subIndex = axis.GetSubAxisIndex();
            if (subIndex < 0)
                return axisNullChar;

            Axis result;
            if (!dict.TryGetValue((uint)subIndex, out result))
                return axisNullChar;

            return result;
        }

        public UnicodeChars GetNullDeviceIcon() => deviceNullChar;

        public UnicodeChars GetDeviceIcon(InputAxis axis, KeyboardLayouts keyboardLayout)
            => GetDeviceIcon(axis.parent, keyboardLayout);

        public UnicodeChars GetDeviceIcon(InputDevice device, KeyboardLayouts keyboardLayout)
        {
            if (device is Keyboard)
                return GetDeviceIcon(keyboardLayout);
            else if (device is Mouse)
                throw new System.NotImplementedException("No mouse layouts supported yet.");
            else if (device is HidDevice)
                return GetDeviceIcon((HidDevice)device);
            else
                return deviceNullChar;
        }

        public UnicodeChars GetDeviceIcon(KeyboardLayouts keyboardLayout)
        {
            UnicodeChars result;
            if (keyboardIcons.TryGetValue(keyboardLayout, out result))
                return result;

            return deviceNullChar;
        }

        public UnicodeChars GetDeviceIcon(HidDevice device)
        {
            MappedHidDevice mappedHid = device as MappedHidDevice;
            if (mappedHid != null)
            {
                UnicodeChars character;
                if (mappedHidIcons.TryGetValue(mappedHid.layout, out character))
                    return character;
            }

            return unmappedHidIcon != null ? unmappedHidIcon.Value : deviceNullChar;
        }
    }
}
