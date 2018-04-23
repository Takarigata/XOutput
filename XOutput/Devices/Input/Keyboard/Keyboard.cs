﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace XOutput.Devices.Input.Keyboard
{
    /// <summary>
    /// Keyboard input device.
    /// </summary>
    public sealed class Keyboard : IInputDevice
    {
        public event DeviceInputChangedHandler InputChanged;
        public event DeviceDisconnectedHandler Disconnected { add { } remove { } }

        public int ButtonCount => Enum.GetValues(typeof(Key)).Length;
        public string DisplayName => LanguageModel.Instance.Translate("Keyboard");
        public bool Connected => true;
        public bool HasDPad => false;

        public IEnumerable<DPadDirection> DPads => new DPadDirection[0];
        public IEnumerable<Enum> Buttons => buttons;
        public IEnumerable<Enum> Axes => new Enum[0];
        public IEnumerable<Enum> Sliders => new Enum[0];
        public int ForceFeedbackCount => 0;

        private Thread inputRefresher;
        private readonly Enum[] buttons;
        private readonly DeviceState state;

        public Keyboard()
        {
            buttons = KeyboardInputHelper.Instance.Buttons.Where(x => x != Key.None).OrderBy(x => x.ToString()).OfType<Enum>().ToArray();
            state = new DeviceState(buttons, 0);
            inputRefresher = new Thread(InputRefresher);
            inputRefresher.Name = "Keyboard input notification";
            inputRefresher.SetApartmentState(ApartmentState.STA);
            inputRefresher.IsBackground = true;
            inputRefresher.Start();
        }
        ~Keyboard()
        {
            Dispose();
        }

        /// <summary>
        /// Disposes all resources.
        /// </summary>
        public void Dispose()
        {
            inputRefresher.Abort();
        }

        public double Get(Enum inputType)
        {
            if (!(inputType is Key))
                throw new ArgumentException();
            return System.Windows.Input.Keyboard.IsKeyDown((Key)inputType) ? 1 : 0;
        }

        /// <summary>
        /// Display name.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return DisplayName;
        }

        public void SetForceFeedback(double big, double small)
        {

        }

        private void InputRefresher()
        {
            try
            {
                while (true)
                {
                    var newValues = buttons.ToDictionary(t => t, t => Get(t));
                    var changedValues = state.SetValues(newValues);
                    if (changedValues.Any())
                        InputChanged?.Invoke(this, new DeviceInputChangedEventArgs(changedValues, new int[0]));
                    Thread.Sleep(1);
                }
            }
            catch (ThreadAbortException) { }
        }
    }
}