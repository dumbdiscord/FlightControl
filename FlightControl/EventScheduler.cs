using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class EventScheduler
        {
            Dictionary<string, Action> Loops;
            public List<MyOwnTuple<int, Action>> eventQueue;
            public EventScheduler()
            {
                eventQueue = new List<MyOwnTuple<int, Action>>();
                Loops = new Dictionary<string, Action>();
            }
            public void Tick()
            {
                for (int i = eventQueue.Count - 1; i >= 0; i--)
                {
                    if (eventQueue[i].Key <= 0)
                    {
                        try
                        {
                            eventQueue[i].Value();
                        }
                        catch { }
                        eventQueue.RemoveAt(i);
                        continue;
                    }
                    eventQueue[i].Key = eventQueue[i].Key - 1;
                }
            }
            public void QueueEvent(Action action, int TickDelay)
            {
                eventQueue.Add(new MyOwnTuple<int, Action>(TickDelay, action));
            }
            public void StartLoop(string name, Action action, int refreshrate)
            {
                if (Loops.ContainsKey(name))
                {
                    return;
                }
                Action x = () =>
                {
                    action();
                    QueueEvent(Loops[name], refreshrate);
                };
                Loops[name] = x;
                x();
            }
            public void RemoveLoop(string name)
            {
                if (Loops.ContainsKey(name))
                {
                    Loops.Remove(name);
                }
            }
            public class MyOwnTuple<A, B>
            {
                public A Key;
                public B Value;
                public MyOwnTuple(A key, B value)
                {
                    Key = key;
                    Value = value;
                }

            }
        }
    }
}
