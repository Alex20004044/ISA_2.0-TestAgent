using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventSocket.Helpers
{
    public abstract class ThreadTimer
    {

        public static void Start(Action action, int TimeoutMillisecond)
        {
            Timer timer = null;
            timer = new Timer(new TimerCallback(delegate (object state)
            {
                //pause timer 
                timer.Change(Timeout.Infinite, Timeout.Infinite);

                action();

                //restart timer and wait
                timer.Change(TimeoutMillisecond, TimeoutMillisecond);
            }), null, 0, 0);
        }

        public static void Start(Func<object, bool> action, int TimeoutMillisecond, object state)
        {
            Timer timer = null;
            timer = new Timer(new TimerCallback(delegate (object stateobj)
            {
                //pause timer 
                timer.Change(Timeout.Infinite, Timeout.Infinite);

                action(stateobj);

                //restart timer and wait
                timer.Change(TimeoutMillisecond, TimeoutMillisecond);
            }), state, 0, 0);
        }

        public static void Start<T>(Func<T, bool> action, int TimeoutMillisecond, T state)
        {
            Timer timer = null;
            timer = new Timer(new TimerCallback(delegate (object stateobj)
            {
                //pause timer 
                timer.Change(Timeout.Infinite, Timeout.Infinite);

                action((T)stateobj);

                //restart timer and wait
                timer.Change(TimeoutMillisecond, TimeoutMillisecond);
            }), state, 0, 0);
        }

        public static void StartAsync(Func<object, Task> action, int TimeoutMillisecond, object state)
        {
            Timer timer = null;
            timer = new Timer(new TimerCallback(async delegate (object stateobj)
            {
                //pause timer 
                timer.Change(Timeout.Infinite, Timeout.Infinite);

                await action(stateobj);

                //restart timer and wait
                timer.Change(TimeoutMillisecond, TimeoutMillisecond);
            }), state, 0, 0);
        }

        public static void StartAsync(Func<Task> action, int TimeoutMillisecond)
        {
            Timer timer = null;
            timer = new Timer(new TimerCallback(async delegate (object stateobj)
            {
                //pause timer 
                timer.Change(Timeout.Infinite, Timeout.Infinite);

                await action();

                //restart timer and wait
                timer.Change(TimeoutMillisecond, TimeoutMillisecond);
            }), null, 0, 0);

        }

        public static void StartAsync<T>(Func<T, Task> action, int TimeoutMillisecond, T state)
        {
            Timer timer = null;
            timer = new Timer(new TimerCallback(async delegate (object stateobj)
            {
                //pause timer 
                timer.Change(Timeout.Infinite, Timeout.Infinite);

                await action((T)stateobj);

                //restart timer and wait
                timer.Change(TimeoutMillisecond, TimeoutMillisecond);
            }), state, 0, 0);
        }

    }
}
