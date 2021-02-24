using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LogicReinc.Asp
{
    /// <summary>
    /// Basic class to add triggers at certain intervals
    /// </summary>
    public class LifeCycle
    {
        private bool _active = false;
        private int _loopID = 0;

        public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromMilliseconds(1000);
        public TimeSpan InitialDelay { get; private set; } = TimeSpan.FromSeconds(10);

        protected DateTime StartTime { get; private set; } = DateTime.Now;


        private List<LifeCycleAction> _actions = new List<LifeCycleAction>();

        public LifeCycle()
        {
            Type type = GetType();

            _actions = type.GetMethods()
                .Where(x => x.GetCustomAttribute<LifeCycleActionAttribute>() != null)
                .Select(x => new LifeCycleAction(this, x, x.GetCustomAttribute<LifeCycleActionAttribute>().Interval))
                .ToList();
        }

        public void Start()
        {
            if (_active)
                return;

            int loopID = 0;
            lock (this)
            {
                _loopID++;
                loopID = _loopID;
            }
            Thread t = new Thread(() =>
            {
                StartTime = DateTime.Now;
                Setup();
                while (_loopID == loopID)
                {
                    _active = true;
                    try
                    {
                        LoopActivity();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Loop Exception: [{ex.GetType().Name}]: {ex.Message}");
                    }
                    Thread.Sleep(UpdateInterval);
                }
                _active = false;
            });
            t.IsBackground = true;
            t.Start();
        }
        public void Stop()
        {
            _loopID++;
        }

        public void LoopActivity()
        {
            foreach (LifeCycleAction action in _actions)
            {
                DateTime now = DateTime.Now;
                if (action.ShouldTrigger(now))
                    action.Trigger();
            }
        }

        protected virtual void Setup()
        {

        }

        protected class LifeCycleAction
        {
            public LifeCycle Parent { get; private set; }
            public MethodInfo Method { get; set; }
            public TimeSpan Interval { get; set; }
            public DateTime LastTrigger { get; set; } = DateTime.MinValue;
            public DateTime NextTrigger => LastTrigger.Add(Interval);

            public LifeCycleAction(LifeCycle parent, MethodInfo method, TimeSpan timespan)
            {
                Parent = parent;
                Method = method;
                Interval = timespan;
            }

            public bool ShouldTrigger(DateTime date)
            {
                return NextTrigger < date;
            }


            public void Trigger()
            {
                LastTrigger = DateTime.Now;
                try
                {
                    Method.Invoke(Parent, new object[] { });
                }
                catch (TargetInvocationException ex)
                {
                    Console.WriteLine($"LifeCycle {Method.Name} Exception [{ex.InnerException.GetType().Name}]: {ex.InnerException.Message}");
                }
            }
        }
    }

    public class LifeCycleActionAttribute : Attribute
    {

        public TimeSpan Interval { get; set; }

        public LifeCycleActionAttribute(int ms)
        {
            Interval = TimeSpan.FromMilliseconds(ms);
        }
    }
}
