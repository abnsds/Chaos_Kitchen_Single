// 改进的事件中心，支持事件缓存
using System.Collections.Generic;

public class ReEventCenter : BaseManager<EventCenter>
{
    private Dictionary<string, IEventInfo> eventDic = new Dictionary<string, IEventInfo>();
    private Dictionary<string, Queue<object>> eventCache = new Dictionary<string, Queue<object>>();
    private HashSet<string> readyEvents = new HashSet<string>();

    // 标记事件依赖已就绪
    //public void MarkEventReady(string eventName)
    //{
    //    readyEvents.Add(eventName);

    //    // 触发缓存的事件
    //    if (eventCache.ContainsKey(eventName) && eventDic.ContainsKey(eventName))
    //    {
    //        var cacheQueue = eventCache[eventName];
    //        while (cacheQueue.Count > 0)
    //        {
    //            var cachedData = cacheQueue.Dequeue();
    //            // 根据类型触发事件
    //            TriggerCachedEvent(eventName, cachedData);
    //        }
    //    }
    //}

    //// 带缓存的事件触发
    //public void EventTriggerWithCache<T>(string name, T info)
    //{
    //    if (readyEvents.Contains(name))
    //    {
    //        // 直接触发
    //        EventTrigger(name, info);
    //    }
    //    else
    //    {
    //        // 缓存事件
    //        if (!eventCache.ContainsKey(name))
    //        {
    //            eventCache[name] = new Queue<object>();
    //        }
    //        eventCache[name].Enqueue(info);
    //    }
    //}
}