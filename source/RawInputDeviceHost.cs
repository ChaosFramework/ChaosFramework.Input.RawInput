using System;
using System.Threading;
using SysCol = System.Collections.Generic;

namespace ChaosFramework.Input.RawInput
{
    public class RawInputDeviceHost : InputDeviceHost
    {
        static readonly TimeSpan SLEEP_TIME = new TimeSpan(0, 0, 0, 0, 1);

        readonly InputCaptureForm form;

        public RawInputDeviceHost(InputContext context)
        {
            form = new InputCaptureForm(context);
        }

        SysCol.IEnumerable<InputDevice> InputDeviceHost.RefreshDeviceList()
        {
            form.deviceList.UpdateDeviceList();
            foreach (RawDevice impl in form.deviceList)
                yield return impl.parent;
        }

        void InputDeviceHost.Update()
        {
            System.Windows.Forms.Application.DoEvents();
            Thread.Sleep(SLEEP_TIME);
        }
    }
}
