using ChaosUtil.Platform.Windows.WinAPI.winuser;
using System.Windows.Forms;

namespace ChaosFramework.Input.Windows
{
    public class InputCaptureForm : Form, IMessageFilter
    {
        public readonly DeviceList deviceList;

        public InputCaptureForm(InputContext input)
        {
            SetParent.Invoke(Handle, SetParent.HWND_MESSAGE);
            deviceList = new DeviceList(input, Handle);
            Application.AddMessageFilter(this);
        }

        bool IMessageFilter.PreFilterMessage(ref Message message)
        {
            switch ((WM)message.Msg)
            {
                case WM.INPUT:
                    deviceList?.ProcessMessage(message);
                    break;
            }

            return false;
        }
    }
}
