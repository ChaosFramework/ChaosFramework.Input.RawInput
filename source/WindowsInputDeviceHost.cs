using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ChaosFramework.Input.RawInput
{
    public class WindowsInputDeviceHost : InputDeviceHost
    {
        static readonly TimeSpan SLEEP_TIME = new TimeSpan(0, 0, 0, 0, 1);

        readonly InputCaptureForm form;

        public WindowsInputDeviceHost(InputContext context)
        {
            form = new InputCaptureForm(context);
        }

        void InputDeviceHost.RefreshDeviceList() => form.deviceList.UpdateDeviceList();

        void InputDeviceHost.Update()
        {
            System.Windows.Forms.Application.DoEvents();
            Thread.Sleep(SLEEP_TIME);
        }

        IEnumerator<InputDevice> IEnumerable<InputDevice>.GetEnumerator() => form.deviceList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => form.deviceList.GetEnumerator();
    }
}
