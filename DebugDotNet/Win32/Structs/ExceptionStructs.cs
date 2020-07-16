using System;
using System.Collections.Generic;
using System.Text;
using DebugDotNet.Win32.Enums;

namespace DebugDotNet.Win32.Structs
{

    /// <summary>
    /// Base class exception helpers are made from
    /// </summary>
    public abstract class ExceptionEventHelperClassBase
    {
        DebugEvent BaseEventVal;
        /// <summary>
        /// Exposes the DebugEvent passed when the class was instanced
        /// </summary>
        protected DebugEvent Event
        {
            get
            {
                return BaseEventVal;
            }
            set
            {
                BaseEventVal = value;
            }
        }
        
        /// <summary>
        /// page error exception + access violation exception to the BaseAccessEnum
        /// </summary>
        /// <param name="VType"></param>
        /// <returns></returns>
        protected static BaseAccessViolation ViolationTypeToEnum(uint VType)
        {
            {
                switch (VType)
                {
                    case 0:
                        return BaseAccessViolation.Read;
                    case 1:
                        return BaseAccessViolation.Write;
                    case 8:
                        return BaseAccessViolation.DEP;
                    default:
                        return BaseAccessViolation.Other;
                }
            }
        }
        /// <summary>
        /// <see cref="CheckEventData(DebugEvent)"/> should set this to true if the event passed. It should also skip checks should this already be true
        /// </summary>
        protected bool EventIsOk { get; set; }

        /// <summary>
        /// make in instance of the class to easier read the event data
        /// </summary>
        /// <param name="EventData">make from this event</param>
        public ExceptionEventHelperClassBase(DebugEvent EventData)
        {
            Event = EventData;
        }

        /// <summary>
        /// Check if the event is one this class supports and throw argumentexceptions or invalidoperation exception if not
        /// </summary>
        /// <param name="CheckThisEvent">check this one</param>
        protected virtual void CheckEventData(DebugEvent CheckThisEvent)
        {
            if (EventIsOk)
                return;

            EventIsOk = true;
        }
        
    }

    /// <summary>
    /// Helper class to make it easer to read an In page Error Exception gotton from Debug Events
    /// </summary>
    public class ExceptionEventHelperInPageError : ExceptionEventHelperClassBase
    {
        /// <summary>
        /// Make in instance of this class from the passed <see cref="DebugEvent"/>.
        /// </summary>
        /// <param name="Event">Make from this event</param>
        public ExceptionEventHelperInPageError(DebugEvent Event) : base(Event)
        {

        }


        /// <summary>
        /// Get the address in the debugged process that contains the inacessible data
        /// </summary>
        public uint VirtualAddressLocation
        {
            get
            {
                return Event.ExceptionInfo.TopLevelException.ExceptionInformation[1];
            }
        }

        /// <summary>
        /// return the underlyine code (if any) that came with the exception
        /// </summary>
        public uint NtStatusCode
        {
            get
            {
                return Event.ExceptionInfo.TopLevelException.ExceptionInformation[2];
            }
        }

        /// <summary>
        /// get the violation type (or -1 if not known to DebugDotNet (this is ExceptionDebugInfo.ExceptionInformation[0]
        /// </summary>
        public BaseAccessViolation ViolationType
        {
            get
            {
                return ViolationTypeToEnum(Event.ExceptionInfo.TopLevelException.ExceptionInformation[0]);
            }
        }

        /// <summary>
        /// Does the event contain a Page Error Exception
        /// </summary>
        /// <param name="CheckThisEvent"></param>
        protected override void CheckEventData(DebugEvent CheckThisEvent)
        {
            if (EventIsOk)
                return;
            if (CheckThisEvent == null)
            {
                throw new ArgumentNullException(nameof(CheckThisEvent));
            }
            if (CheckThisEvent.dwDebugEventCode != DebugEventType.ExceptionDebugEvent)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException(nameof(CheckThisEvent) + "does not contain a DebugEventType.ExceptionDebugEvent");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }
            if (CheckThisEvent.ExceptionInfo.TopLevelException.ExceptionCode != ExceptionCode.ExceptionInPageError)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException(nameof(CheckThisEvent) + "is not a Page error exception");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            base.CheckEventData(CheckThisEvent);
        }

    }
    /// <summary>
    /// Wrapper to interpret ExceptionAcessViolationData
    /// WE ASSUME the constructor has already done the checking on the type of event this is
    /// </summary>
    public class ExceptionEventHelperAccessViolation: ExceptionEventHelperClassBase
    {

 
        /// <summary>
        /// Check is the Event passed contains an exception of ExceptionAcessViolation
        /// </summary>
        /// <param name="Event">check this event</param>
        protected override void CheckEventData(DebugEvent Event)
        {
            if (EventIsOk)
            {
                return;
            }
            if (Event == null)
            {
                throw new ArgumentNullException(nameof(Event));
            }
            else
            {
                if (Event.dwDebugEventCode != DebugEventType.ExceptionDebugEvent)
                {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                    throw new ArgumentException(nameof(Event) + "does not contain a DebugEventType.ExceptionDebugEvent");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                }
                else
                {
                    if (Event.ExceptionInfo.TopLevelException.ExceptionCode != ExceptionCode.ExceptionAccessViolation)
                    {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                        throw new ArgumentException(nameof(Event) + "does not containa ExceptionCode.ExceptionAccessViolation ");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                    }
                }

            }
            EventIsOk = true;
        }
        /// <summary>
        /// Make in instance of this helper class to interpret an ExceptionAcessViolation 
        /// </summary>
        /// <param name="Event"></param>
        public ExceptionEventHelperAccessViolation(DebugEvent Event): base(Event)
        {
            CheckEventData(Event);
        }


        /// <summary>
        /// Get the address in the debugged process that contains the inacessible data
        /// </summary>
        public uint VirtualAddressLocation
        { 
            get
            {
                return Event.ExceptionInfo.TopLevelException.ExceptionInformation[1];
            }
        }


        /// <summary>
        /// get the violation type (or -1 if not known to DebugDotNet (this is ExceptionDebugInfo.ExceptionInformation[0]
        /// </summary>
        public BaseAccessViolation ViolationType
        {
            get
            {
                return ViolationTypeToEnum(Event.ExceptionInfo.TopLevelException.ExceptionInformation[0]);
            }
        }

    }


}
