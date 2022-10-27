using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;

namespace TileService
{
    public class EventSourceEnumerator : System.Diagnostics.Tracing.EventListener
    {
        private static EventSourceEnumerator? instance = null;

        public static async void Run(Func<Task> action)
        {
            instance = new EventSourceEnumerator();
            await action.Invoke();
            instance.dumpEvents();
        }

        private Dictionary<EventSource, Dictionary<int,EventInfo>> _seenEvents = new Dictionary<EventSource, Dictionary<int,EventInfo>>();

        private EventSourceEnumerator()
        {
            this.EventSourceCreated += EventSourceCreatedHandler;
            this.EventWritten += EventWrittenHandler;
        }

        private void EventWrittenHandler(object? sender, System.Diagnostics.Tracing.EventWrittenEventArgs e)
        {
            var events = _seenEvents[e.EventSource];
            if (events != null)
            {
                if (!events.ContainsKey(e.EventId))
                {
                    events.Add(e.EventId, new EventInfo() { Id = e.EventId, Level = e.Level, Name = e.EventName, Count =1 });
                }
                else
                {
                    var ei = events[e.EventId];
                    Interlocked.Increment(ref ei.Count);
                }
            }
        }

        void EventSourceCreatedHandler(object? sender, System.Diagnostics.Tracing.EventSourceCreatedEventArgs e)
        {
            if (!_seenEvents.ContainsKey(e.EventSource))
            {
                this.EnableEvents(e.EventSource, System.Diagnostics.Tracing.EventLevel.Verbose);
                _seenEvents.Add(e.EventSource, new Dictionary<int, EventInfo>());
            }
        }

        private void dumpEvents()
        {
            Console.WriteLine();
            Console.WriteLine("=== Events Seen ===");
            Console.WriteLine("Provider Name : Event Name : Event ID : Event Level (int) : Count");
            Console.WriteLine("-----------------------------------------------------------------");
            foreach (var es in _seenEvents )
            {
                foreach (var ei in es.Value.Values)
                {
                    Console.WriteLine($"{es.Key.Name} : {ei.Name} : {ei.Id} : {ei.Level} ({(int) ei.Level}) : {ei.Count}");
                }
            }
        }


        private class EventInfo
        {
            public int Id;
            public string Name;
            public EventLevel Level;
            public int Count;
        }

    }
}


