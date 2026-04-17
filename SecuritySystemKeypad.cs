using System;
using System.Collections.Generic;
using Crestron.RAD.Common.Enums;
using Crestron.RAD.Common.Events;
using Crestron.RAD.Common.Interfaces;
using Crestron.RAD.DeviceTypes.SecuritySystem;
using Crestron.SimplSharp;

namespace SecuritySystem_Elk_M1_IP_v1
{
    /// <summary>
    /// This class is used to define the security system keypad
    /// </summary>
    public class SecuritySystemKeypad : IEmulatedSecuritySystemKeypad, IDisposable
    {
        #region Fields

        protected SecuritySystemProtocol SecuritySystemProtocol;
        protected CTimer ArrowKeyRampTimer;
        protected ArrowDirections ArrowKeyRampingDirection;
        protected bool ArrowKeyIsRamping;
        private int _rampingTickRate = 500;
        private string _statusText;

        #endregion

        #region Ctor

        /// <summary>
        /// Default constructor
        /// </summary>
        public SecuritySystemKeypad()
        {
            Leds = new SecuritySystemKeypadLed[2];
            Leds[0] = new SecuritySystemKeypadLed(0)
            {
                State = new SecuritySystemKeypadIndicatorState(),
                Label = "Armed",
                Color = SecuritySystemKeypadLedColors.Red
            };

            Leds[1] = new SecuritySystemKeypadLed(1)
            {
                State = new SecuritySystemKeypadIndicatorState(),
                Label = "Ready",
                Color = SecuritySystemKeypadLedColors.Green
            };

            FunctionButtons = new SecuritySystemKeypadFunctionButton[3];
            FunctionButtons[0] = new SecuritySystemKeypadFunctionButton(0)
            {
                FunctionType = SecuritySystemKeypadFunctionType.Function,
                Icon = SecuritySystemKeypadFunctionButtonIcon.Stay,
                Label = new KeypadLabels() { PrimaryLabel = "Stay" }
            };

            FunctionButtons[1] = new SecuritySystemKeypadFunctionButton(1)
            {
                FunctionType = SecuritySystemKeypadFunctionType.Function,
                Icon = SecuritySystemKeypadFunctionButtonIcon.Away,
                Label = new KeypadLabels() { PrimaryLabel = "Away" }
            };

            FunctionButtons[2] = new SecuritySystemKeypadFunctionButton(2)
            {
                FunctionType = SecuritySystemKeypadFunctionType.Function,
                Icon = SecuritySystemKeypadFunctionButtonIcon.Fire,
                Label = new KeypadLabels() { PrimaryLabel = "Fire" }
            };
        }

        #endregion

        #region Property

        /// <summary>
        /// Keypad may need to be constructed before the protocol exists. This allows the flexibility.
        /// </summary>
        protected bool Initialized
        {
            get { return SecuritySystemProtocol != null; }
        }

        #endregion

        #region Events

        public event StateChangeHandler StateChange;
        public event EventHandler<SecuritySystemKeypadTextChangedEventArgs> SecuritysystemKeypadTextChanged;

        #endregion

        #region Public/protected Method

        /// <summary>
        /// Initialize the security system protocol
        /// </summary>
        /// <param name="protocol">Security system protocol</param>
        public void Initialize(SecuritySystemProtocol protocol)
        {
            if (SecuritySystemProtocol != null)
            {
                SecuritySystemProtocol.StateChange -= SecuritySystemProtocolOnStateChange;
                SecuritySystemProtocol.AlarmChange -= SecuritySystemProtocolOnAlarmChange;
                SecuritySystemProtocol.ReadyChanged -= SecuritySystemProtocolOnReadyChanged;
            }

            SecuritySystemProtocol = protocol;
            if (SecuritySystemProtocol != null)
            {
                SecuritySystemProtocol.StateChange += SecuritySystemProtocolOnStateChange;
                SecuritySystemProtocol.AlarmChange += SecuritySystemProtocolOnAlarmChange;
                SecuritySystemProtocol.ReadyChanged += SecuritySystemProtocolOnReadyChanged;
            }
        }

        /// <summary>
        /// Dispose the object
        /// </summary>
        public void Dispose()
        {
            if (ArrowKeyRampTimer != null)
            {
                ArrowKeyRampTimer.Stop();
                ArrowKeyRampTimer.Dispose();
                ArrowKeyRampTimer = null;
            }

            if (SecuritySystemProtocol != null)
            {
                SecuritySystemProtocol.StateChange -= SecuritySystemProtocolOnStateChange;
                SecuritySystemProtocol.AlarmChange -= SecuritySystemProtocolOnAlarmChange;
                SecuritySystemProtocol.ReadyChanged -= SecuritySystemProtocolOnReadyChanged;
            }
        }

        #endregion

        #region Numeric Keypad

        /// <summary>
        /// Property indicating that the KeypadNumber command is supported.
        /// </summary>
        public bool SupportsKeypadNumber
        {
            get { return false; }
        }

        /// <summary>
        /// Sends a keypad number to the device.
        /// </summary>
        /// <param name="number">Number to be sent to the device.</param>
        public void KeypadNumber(uint num)
        {
            if (!Initialized)
            {
                return;
            }

            if (!SupportsKeypadNumber)
            {
                SecuritySystemProtocol.LogMessage("SecuritySystem does not support KeypadNumber.");
                return;
            }

            SecuritySystemProtocol.SendKeypadNumber(num);
        }

        /// <summary>
        /// Property indicating that the Keypad Pound command is supported.
        /// </summary>
        public bool SupportsPound
        {
            get { return false; }
        }

        /// <summary>
        /// Method to send a Keypad "#" to the device.
        /// </summary>
        public void Pound()
        {
            if (!Initialized)
            {
                return;
            }

            if (!SupportsPound)
            {
                SecuritySystemProtocol.LogMessage("SecuritySystem does not support Keypad Pound.");
                return;
            }

            SecuritySystemProtocol.SendKeypadPound();
        }

        /// <summary>
        /// Property indicating that the Keypad Asterisk command is supported.
        /// </summary>
        public bool SupportsAsterisk
        {
            get { return false; }
        }

        /// <summary>
        /// Method to send a Keypad "*" to the device.
        /// </summary>
        public void Asterisk()
        {
            if (!Initialized)
            {
                return;
            }

            if (!SupportsAsterisk)
            {
                SecuritySystemProtocol.LogMessage("SecuritySystem does not support Keypad Asterisk.");
                return;
            }

            SecuritySystemProtocol.SendKeypadAsterisk();
        }

        /// <summary>
        /// Property indicating that the Keypad Period command is supported.
        /// </summary>
        public bool SupportsPeriod
        {
            get { return false; }
        }

        /// <summary>
        /// Method to send a Keypad "." to the device.
        /// </summary>
        public void Period()
        {
            if (!Initialized)
            {
                return;
            }

            if (!SupportsPeriod)
            {
                SecuritySystemProtocol.LogMessage("SecuritySystem does not support Period.");
                return;
            }

            SecuritySystemProtocol.SendKeypadPeriod();
        }

        /// <summary>
        /// Property indicating that the Keypad Dash command is supported.
        /// </summary>
        public bool SupportsDash
        {
            get { return false; }
        }

        /// <summary>
        /// Method to send a Keypad "-" to the device.
        /// </summary>
        public void Dash()
        {
            if (!Initialized)
            {
                return;
            }

            if (!SupportsDash)
            {
                SecuritySystemProtocol.LogMessage("SecuritySystem does not support Dash.");
                return;
            }

            SecuritySystemProtocol.SendKeypadDash();
        }

        /// <summary>
        /// Method to send a series of keypad characters to the device.
        /// </summary>
        /// <param name="keys"></param>
        public void SendKeypadString(string keys)
        {
            if (!Initialized)
            {
                return;
            }

            SecuritySystemProtocol.SendKeypadString(keys);
        }

        /// <summary>
        /// Property indicating that the Keypad Back Space command is supported.
        /// </summary>
        public bool SupportsKeypadBackSpace
        {
            get { return true; }
        }

        /// <summary>
        /// Method to send a Back Space to the device.
        /// </summary>
        public void KeypadBackSpace()
        {
            if (!Initialized)
            {
                return;
            }

            if (!SupportsKeypadBackSpace)
            {
                SecuritySystemProtocol.LogMessage("SecuritySystem does not support Keypad Back Space.");
                return;
            }

            SecuritySystemProtocol.SendKeypadBackSpace();
        }

        /// <summary>
        /// Property defining the "standard" labels for a numeric keypad buttons 0-9
        /// </summary>
        public KeypadLabels[] NumericKeypadLabels
        {
            get
            {
                return new[]
                {
                    new KeypadLabels { PrimaryLabel = "0", SecondaryLabel = "" },
                    new KeypadLabels { PrimaryLabel = "1", SecondaryLabel = "Arm" },
                    new KeypadLabels { PrimaryLabel = "2", SecondaryLabel = "Disarm" },
                    new KeypadLabels { PrimaryLabel = "3", SecondaryLabel = "Attribute" },
                    new KeypadLabels { PrimaryLabel = "4", SecondaryLabel = "" },
                    new KeypadLabels { PrimaryLabel = "5", SecondaryLabel = "" },
                    new KeypadLabels { PrimaryLabel = "6", SecondaryLabel = "" },
                    new KeypadLabels { PrimaryLabel = "7", SecondaryLabel = "" },
                    new KeypadLabels { PrimaryLabel = "8", SecondaryLabel = "" },
                    new KeypadLabels { PrimaryLabel = "9", SecondaryLabel = "" }
                };
            }
        }

        public KeypadLabels DashLabels
        {
            get
            {
                return new KeypadLabels
                {
                    PrimaryLabel = "-",
                    SecondaryLabel = ""
                };
            }
        }

        public KeypadLabels PeriodLabels
        {
            get
            {
                return new KeypadLabels
                {
                    PrimaryLabel = ".",
                    SecondaryLabel = ""
                };
            }
        }

        public KeypadLabels AsteriskLabels
        {
            get
            {
                return new KeypadLabels
                {
                    PrimaryLabel = "*",
                    SecondaryLabel = ""
                };
            }
        }

        public KeypadLabels PoundLabels
        {
            get
            {
                return new KeypadLabels
                {
                    PrimaryLabel = "#",
                    SecondaryLabel = ""
                };
            }
        }

        #endregion

        #region Navigation

        public bool SupportsArrowKeys
        {
            get { return false; }
        }

        public List<ArrowDirections> ArrowKeysSupported
        {
            get
            {
                return new List<ArrowDirections>()
                {
                    ArrowDirections.Down,
                    ArrowDirections.Up,
                    ArrowDirections.Right,
                    ArrowDirections.Left
                };
            }
        }

        public bool SupportsSelect
        {
            get { return false; }
        }

        public void ArrowKey(ArrowDirections direction, CommandAction action)
        {
            if (!SupportsArrowKeys)
            {
                SecuritySystemProtocol.LogMessage("SecuritySystem : The command ArrowKey is not supported");
                return;
            }

            switch (action)
            {
                case CommandAction.Hold:
                    PressArrowKey(direction);
                    break;

                case CommandAction.Release:
                    ReleaseArrowKey();
                    break;

                case CommandAction.None:
                    ArrowKey(direction);
                    break;
            }
        }

        public void ArrowKey(ArrowDirections direction)
        {
            if (!Initialized)
            {
                return;
            }

            SecuritySystemProtocol.SendKeypadArrowKeys(direction);
        }

        public void PressArrowKey(ArrowDirections direction)
        {
            if (ArrowKeyRampTimer == null)
            {
                ArrowKeyRampTimer = new CTimer(ArrowKeyTick, null, 0, _rampingTickRate);
            }

            ArrowKeyRampingDirection = direction;
            ArrowKeyIsRamping = true;
        }

        public void ReleaseArrowKey()
        {
            if (ArrowKeyRampTimer == null)
            {
                return;
            }

            ArrowKeyRampTimer.Stop();
            ArrowKeyRampTimer.Dispose();
            ArrowKeyRampTimer = null;
            ArrowKeyIsRamping = false;
        }

        protected void ArrowKeyTick(object obj)
        {
            if (ArrowKeyIsRamping)
            {
                ArrowKey(ArrowKeyRampingDirection);
            }
        }

        public void Select()
        {
            if (!SupportsSelect)
            {
                SecuritySystemProtocol.LogMessage("SecuritySystem : The command Select is not supported");
                return;
            }
        }

        public bool SupportsEnter
        {
            get { return true; }
        }

        public void Enter()
        {
            if (!Initialized)
            {
                return;
            }

            if (!SupportsEnter)
            {
                SecuritySystemProtocol.LogMessage("SecuritySystem : The command Enter is not supported");
                return;
            }

            SecuritySystemProtocol.SendKeypadEnter();
        }

        public bool SupportsClear
        {
            get { return true; }
        }

        public void Clear()
        {
            if (!Initialized)
            {
                return;
            }

            if (!SupportsClear)
            {
                SecuritySystemProtocol.LogMessage("SecuritySystem : The command Clear is not supported");
                return;
            }

            SecuritySystemProtocol.SendKeypadClear();
        }

        public bool SupportsExit
        {
            get { return false; }
        }

        public void Exit()
        {
            if (!Initialized)
            {
                return;
            }

            if (!SupportsExit)
            {
                SecuritySystemProtocol.LogMessage("SecuritySystem : The command Exit is not supported");
                return;
            }

            SecuritySystemProtocol.SendKeypadExit();
        }

        public bool SupportsHome
        {
            get { return false; }
        }

        public void Home()
        {
            if (!Initialized)
            {
                return;
            }

            if (!SupportsHome)
            {
                SecuritySystemProtocol.LogMessage("SecuritySystem : The command Home is not supported");
                return;
            }

            SecuritySystemProtocol.SendKeypadHome();
        }

        public bool SupportsMenu
        {
            get { return false; }
        }

        public void Menu()
        {
            if (!Initialized)
            {
                return;
            }

            if (!SupportsMenu)
            {
                SecuritySystemProtocol.LogMessage("SecuritySystem : The command Menu is not supported");
                return;
            }

            SecuritySystemProtocol.SendKeypadMenu();
        }

        #endregion

        #region Keypad Status Text

        public bool SupportsKeypadStatusText
        {
            get { return true; }
        }

        #endregion

        #region IEmulatedSecuritySystemKeypad Implementation

        public SecuritySystemKeypadFunctionButton[] FunctionButtons { get; protected set; }

        public SecuritySystemKeypadLed[] Leds { get; protected set; }

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                _statusText = value;

                var textChangedEventArgs = new SecuritySystemKeypadTextChangedEventArgs()
                {
                    KeypadText = _statusText
                };

                var handler = SecuritysystemKeypadTextChanged;
                if (handler != null)
                {
                    handler(this, textChangedEventArgs);
                }
            }
        }

        public bool SupportsFunctionButtons
        {
            get { return true; }
        }

        public bool SupportsLeds
        {
            get { return true; }
        }

        public bool SupportsTextualDisplay
        {
            get { return true; }
        }

        public void TriggerFunctionButton(int buttonNumber)
        {
            if (!Initialized)
            {
                return;
            }

            SecuritySystemProtocol.TriggerFunctionButton(buttonNumber);
        }

        #endregion

        #region Private Method

        private void FireStateChangedEvent(SecuritySystemState eventType, bool state)
        {
            var stateObj = new SecuritySystemStateArgs
            {
                EventType = eventType,
                State = state
            };

            var handler = StateChange;
            if (handler != null)
            {
                handler(stateObj);
            }
        }

        private void SetReadyLedOnState()
        {
            SecuritySystemKeypadIndicatorState indicatorState = new SecuritySystemKeypadIndicatorState();
            indicatorState.Index = 1;
            indicatorState.State = SecuritySystemKeypadIndicatorStateType.On;
            Leds[1].State = indicatorState;
        }

        private void SetReadyLedOffState()
        {
            SecuritySystemKeypadIndicatorState indicatorState = new SecuritySystemKeypadIndicatorState();
            indicatorState.Index = 1;
            indicatorState.State = SecuritySystemKeypadIndicatorStateType.Off;
            Leds[1].State = indicatorState;
        }

        private void SecuritySystemProtocolOnAlarmChange(object changedObject)
        {
            var obj = changedObject as SecuritySystemAlarmStateArgs;
            if (obj != null && obj.State)
            {
                if (obj.Alarm.AlarmType == SecuritySystemAlarmType.Fire)
                {
                    StatusText = "";
                }
            }
        }

        private void SecuritySystemProtocolOnReadyChanged(object sender, ValueEventArgs<bool> e)
        {
            if (e == null)
            {
                return;
            }

            if (e.Value)
            {
                SetReadyLedOnState();
                if (string.IsNullOrWhiteSpace(StatusText))
                {
                    StatusText = "Ready";
                }
            }
            else
            {
                SetReadyLedOffState();
            }
        }

        private void SecuritySystemProtocolOnStateChange(object changedObject)
        {
            var obj = changedObject as SecuritySystemStateArgs;
            if (obj == null)
            {
                return;
            }

            UpdateLedIndicator(0, obj.EventType, obj.State);
            FireStateChangedEvent(obj.EventType, obj.State);

            if (obj.State)
            {
                StatusText = obj.EventType.ToString();
            }
        }

        private void UpdateLedIndicator(int index, SecuritySystemState eventType, bool state)
        {
            SecuritySystemKeypadIndicatorStateType type = SecuritySystemKeypadIndicatorStateType.Unknown;

            switch (eventType)
            {
                case SecuritySystemState.ArmedStay:
                case SecuritySystemState.ArmedAway:
                    type = state
                        ? SecuritySystemKeypadIndicatorStateType.On
                        : SecuritySystemKeypadIndicatorStateType.Off;
                    break;

                case SecuritySystemState.Disarmed:
                    type = SecuritySystemKeypadIndicatorStateType.Off;
                    break;
            }

            if (type == SecuritySystemKeypadIndicatorStateType.Unknown)
            {
                return;
            }

            SecuritySystemKeypadLed led = new SecuritySystemKeypadLed(index);
            SecuritySystemKeypadIndicatorState indicatorState = new SecuritySystemKeypadIndicatorState();
            indicatorState.Index = index;
            indicatorState.State = type;
            led.State = indicatorState;
            ToggleLedState(led);
        }

        private void PrintSampleUserAttribute()
        {
            SecuritySystemProtocol.PrintAttributeValue();
        }

        private void ToggleLedState(ISecuritySystemKeypadLed newLed)
        {
            SecuritySystemProtocol.LogMessage("ToggleLedState method is called for led index " + newLed.Index + "  Label : " + newLed.Label);

            if (Leds == null)
            {
                return;
            }

            if (Leds[newLed.Index].State != newLed.State)
            {
                Leds[newLed.Index].State = newLed.State;
            }
        }

        #endregion
    }
}