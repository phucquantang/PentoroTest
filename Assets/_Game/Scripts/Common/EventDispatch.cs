using System;
using System.Collections.Generic;
using UnityEngine;

namespace Common
{
    public class EventDispatchName
    {
        public static string TileClicked = "TileClicked";
        public static string Lose = "Lose";
        public static string Win = "Win";
        public static string GemMatched = "GemMatched";
        public static string GameStart = "GameStart";
    }

    public class EventDispatch
    {
        protected static readonly Dictionary<string, List<EventListenerData>> eventAction = new();

        protected class EventListenerData
        {
            public GameObject owner;
            public Action<object> callback;

            public EventListenerData(GameObject owner, Action<object> callback)
            {
                this.owner = owner;
                this.callback = callback;
            }
        }

        public static void AddListener(GameObject owner, Action<object> listener, string eventName)
        {
            if (!eventAction.ContainsKey(eventName))
            {
                eventAction[eventName] = new List<EventListenerData>();
            }
            eventAction[eventName].Add(new EventListenerData(owner, listener));
        }

        public static void RemoveListener(Action<object> listener, string eventName)
        {
            if (eventAction.ContainsKey(eventName))
            {
                eventAction[eventName].RemoveAll(item => item.callback == listener);

                if (eventAction[eventName].Count == 0)
                {
                    eventAction.Remove(eventName);
                }
            }
        }

        public static void RemoveAllListener(GameObject owner)
        {
            foreach (var eventName in new List<string>(eventAction.Keys))
            {
                eventAction[eventName].RemoveAll(item => item.owner == owner);

                if (eventAction[eventName].Count == 0)
                {
                    eventAction.Remove(eventName);
                }
            }
        }

        public static void Dispatch(string eventName, object param = null)
        {
            if (eventAction.ContainsKey(eventName))
            {
                foreach (var listener in eventAction[eventName])
                {
                    listener.callback?.Invoke(param);
                }
            }
        }
    }
}
