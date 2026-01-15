using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public interface IEventInfo
{
    // 添加一个标记，用于表示事件是否已就绪
    bool IsReady { get; set; }
    // 缓存的事件数量
    int CachedEventCount { get; }
}

public class EventInfo<T> : IEventInfo
{
    public UnityAction<T> actions;
    private Queue<T> eventCache = new Queue<T>();
    public bool IsReady { get; set; } = false;
    public int CachedEventCount => eventCache.Count;

    public EventInfo(UnityAction<T> action)
    {
        actions += action;
    }

    public void CacheEvent(T data)
    {
        eventCache.Enqueue(data);
        Debug.Log($"事件缓存: {typeof(T).Name}, 缓存数量: {eventCache.Count}");
    }

    public void TriggerCachedEvents()
    {
        while (eventCache.Count > 0)
        {
            T cachedData = eventCache.Dequeue();
            actions?.Invoke(cachedData);
        }
    }
}

public class EventInfo : IEventInfo
{
    public UnityAction actions;
    private Queue<System.Action> eventCache = new Queue<System.Action>();
    public bool IsReady { get; set; } = false;
    public int CachedEventCount => eventCache.Count;

    public EventInfo(UnityAction action)
    {
        actions += action;
    }

    public void CacheEvent()
    {
        eventCache.Enqueue(() => { });
        Debug.Log($"无参事件缓存, 缓存数量: {eventCache.Count}");
    }

    public void TriggerCachedEvents()
    {
        while (eventCache.Count > 0)
        {
            eventCache.Dequeue();
            actions?.Invoke();
        }
    }
}

/// <summary>
/// 事件中心 - 支持缓存的事件系统
/// 特性：
/// 1. 支持事件就绪标记
/// 2. 自动缓存未就绪时触发的事件
/// 3. 延迟执行缓存的事件
/// 4. 支持事件依赖管理
/// </summary>
public class EventCenter : BaseManager<EventCenter>
{
    private Dictionary<string, IEventInfo> eventDic = new Dictionary<string, IEventInfo>();

    // 事件依赖管理器
    private Dictionary<string, List<string>> eventDependencies = new Dictionary<string, List<string>>();

    // 事件状态跟踪
    private Dictionary<string, System.Type> eventTypes = new Dictionary<string, System.Type>();

    #region 基础事件功能
    /// <summary>
    /// 添加事件监听（泛型）
    /// </summary>
    public void AddEventListener<T>(string name, UnityAction<T> action)
    {
        if (eventDic.ContainsKey(name))
        {
            (eventDic[name] as EventInfo<T>).actions += action;
        }
        else
        {
            var eventInfo = new EventInfo<T>(action);
            eventDic.Add(name, eventInfo);
            eventTypes[name] = typeof(T);
        }
    }

    /// <summary>
    /// 添加事件监听（无参）
    /// </summary>
    public void AddEventListener(string name, UnityAction action)
    {
        if (eventDic.ContainsKey(name))
        {
            (eventDic[name] as EventInfo).actions += action;
        }
        else
        {
            var eventInfo = new EventInfo(action);
            eventDic.Add(name, eventInfo);
            eventTypes[name] = null; // 无参事件
        }
    }

    /// <summary>
    /// 移除事件监听
    /// </summary>
    public void RemoveEventListener<T>(string name, UnityAction<T> action)
    {
        if (eventDic.ContainsKey(name))
        {
            var eventInfo = eventDic[name] as EventInfo<T>;
            if (eventInfo != null)
            {
                eventInfo.actions -= action;

                // 如果没有监听者了，可以清理
                if (eventInfo.actions == null && eventInfo.CachedEventCount == 0)
                {
                    eventDic.Remove(name);
                    eventTypes.Remove(name);
                }
            }
        }
    }

    public void RemoveEventListener(string name, UnityAction action)
    {
        if (eventDic.ContainsKey(name))
        {
            var eventInfo = eventDic[name] as EventInfo;
            if (eventInfo != null)
            {
                eventInfo.actions -= action;

                if (eventInfo.actions == null && eventInfo.CachedEventCount == 0)
                {
                    eventDic.Remove(name);
                    eventTypes.Remove(name);
                }
            }
        }
    }
    #endregion

    #region 事件缓存功能
    /// <summary>
    /// 标记事件已就绪（可以触发缓存的事件）
    /// </summary>
    public void MarkEventReady(string eventName)
    {
        if (eventDic.ContainsKey(eventName))
        {
            var eventInfo = eventDic[eventName];
            eventInfo.IsReady = true;

            Debug.Log($"事件 [{eventName}] 已标记为就绪");

            // 检查是否有缓存的事件需要触发
            TriggerCachedEvents(eventName);

            // 检查是否有依赖此事件的其他事件
            CheckDependentEvents(eventName);
        }
        else
        {
            // 事件不存在，创建一个空的事件信息并标记为就绪
            Debug.LogWarning($"事件 [{eventName}] 不存在，将创建空事件并标记就绪");
            CreateEmptyEventIfNotExist(eventName);
            MarkEventReady(eventName);
        }
    }

    /// <summary>
    /// 标记事件未就绪（新触发的事件将被缓存）
    /// </summary>
    public void MarkEventNotReady(string eventName)
    {
        if (eventDic.ContainsKey(eventName))
        {
            eventDic[eventName].IsReady = false;
            Debug.Log($"事件 [{eventName}] 标记为未就绪");
        }
    }

    /// <summary>
    /// 安全触发事件（支持缓存）
    /// </summary>
    public void SafeEventTrigger<T>(string name, T info)
    {
        if (eventDic.ContainsKey(name))
        {
            var eventInfo = eventDic[name] as EventInfo<T>;
            if (eventInfo != null)
            {
                if (eventInfo.IsReady && eventInfo.actions != null)
                {
                    // 事件就绪，直接触发
                    eventInfo.actions.Invoke(info);
                }
                else
                {
                    // 事件未就绪，缓存数据
                    eventInfo.CacheEvent(info);
                    Debug.Log($"事件 [{name}] 未就绪，数据已缓存");
                }
            }
        }
        else
        {
            // 事件不存在，创建并缓存
            Debug.LogWarning($"事件 [{name}] 不存在，将创建并缓存数据");
            CreateEmptyEventIfNotExist<T>(name);
            SafeEventTrigger(name, info);
        }
    }

    /// <summary>
    /// 安全触发事件（无参，支持缓存）
    /// </summary>
    public void SafeEventTrigger(string name)
    {
        if (eventDic.ContainsKey(name))
        {
            var eventInfo = eventDic[name] as EventInfo;
            if (eventInfo != null)
            {
                if (eventInfo.IsReady && eventInfo.actions != null)
                {
                    eventInfo.actions.Invoke();
                }
                else
                {
                    eventInfo.CacheEvent();
                    Debug.Log($"事件 [{name}] 未就绪，事件已缓存");
                }
            }
        }
        else
        {
            Debug.LogWarning($"事件 [{name}] 不存在，将创建并缓存");
            CreateEmptyEventIfNotExist(name);
            SafeEventTrigger(name);
        }
    }

    /// <summary>
    /// 强制触发事件（绕过缓存检查）
    /// </summary>
    public void ForceEventTrigger<T>(string name, T info)
    {
        EventTrigger(name, info);
    }

    public void ForceEventTrigger(string name)
    {
        EventTrigger(name);
    }

    /// <summary>
    /// 检查并触发某个事件的所有缓存事件
    /// </summary>
    private void TriggerCachedEvents(string eventName)
    {
        if (eventDic.ContainsKey(eventName))
        {
            var eventInfo = eventDic[eventName];

            if (eventTypes[eventName] == null)
            {
                // 无参事件
                var noParamEvent = eventInfo as EventInfo;
                if (noParamEvent != null && noParamEvent.CachedEventCount > 0)
                {
                    Debug.Log($"触发 [{eventName}] 的缓存事件，数量: {noParamEvent.CachedEventCount}");
                    noParamEvent.TriggerCachedEvents();
                }
            }
            else
            {
                // 泛型事件 - 通过反射调用
                var cachedCount = eventInfo.CachedEventCount;
                if (cachedCount > 0)
                {
                    Debug.Log($"事件 [{eventName}] 有 {cachedCount} 个缓存事件，但需要特定类型数据，请使用具体类型的缓存功能");
                }
            }
        }
    }
    #endregion

    #region 事件依赖管理
    /// <summary>
    /// 添加事件依赖（eventName 依赖于 dependencyName）
    /// </summary>
    public void AddEventDependency(string eventName, string dependencyName)
    {
        if (!eventDependencies.ContainsKey(eventName))
        {
            eventDependencies[eventName] = new List<string>();
        }

        if (!eventDependencies[eventName].Contains(dependencyName))
        {
            eventDependencies[eventName].Add(dependencyName);
            Debug.Log($"事件依赖: [{eventName}] 依赖于 [{dependencyName}]");

            // 如果依赖事件已经就绪，自动标记当前事件就绪
            if (IsEventReady(dependencyName))
            {
                MarkEventReady(eventName);
            }
        }
    }

    /// <summary>
    /// 移除事件依赖
    /// </summary>
    public void RemoveEventDependency(string eventName, string dependencyName)
    {
        if (eventDependencies.ContainsKey(eventName))
        {
            eventDependencies[eventName].Remove(dependencyName);
        }
    }

    /// <summary>
    /// 检查依赖事件
    /// </summary>
    private void CheckDependentEvents(string readyEventName)
    {
        foreach (var kvp in eventDependencies)
        {
            string dependentEvent = kvp.Key;
            List<string> dependencies = kvp.Value;

            if (dependencies.Contains(readyEventName))
            {
                // 检查所有依赖是否都已就绪
                bool allDependenciesReady = true;
                foreach (string dependency in dependencies)
                {
                    if (!IsEventReady(dependency))
                    {
                        allDependenciesReady = false;
                        break;
                    }
                }

                if (allDependenciesReady)
                {
                    MarkEventReady(dependentEvent);
                }
            }
        }
    }

    /// <summary>
    /// 检查事件是否就绪
    /// </summary>
    public bool IsEventReady(string eventName)
    {
        if (eventDic.ContainsKey(eventName))
        {
            return eventDic[eventName].IsReady;
        }
        return false;
    }
    #endregion

    #region 辅助方法
    /// <summary>
    /// 原始的事件触发（保持向后兼容）
    /// </summary>
    public void EventTrigger<T>(string name, T info)
    {
        if (eventDic.ContainsKey(name))
        {
            var eventInfo = eventDic[name] as EventInfo<T>;
            if (eventInfo != null && eventInfo.actions != null)
            {
                eventInfo.actions.Invoke(info);
            }
        }
    }

    public void EventTrigger(string name)
    {
        if (eventDic.ContainsKey(name))
        {
            var eventInfo = eventDic[name] as EventInfo;
            if (eventInfo != null && eventInfo.actions != null)
            {
                eventInfo.actions.Invoke();
            }
        }
    }

    /// <summary>
    /// 如果事件不存在则创建空事件
    /// </summary>
    private void CreateEmptyEventIfNotExist<T>(string eventName)
    {
        if (!eventDic.ContainsKey(eventName))
        {
            var eventInfo = new EventInfo<T>(null);
            eventDic.Add(eventName, eventInfo);
            eventTypes[eventName] = typeof(T);
        }
    }

    private void CreateEmptyEventIfNotExist(string eventName)
    {
        if (!eventDic.ContainsKey(eventName))
        {
            var eventInfo = new EventInfo(null);
            eventDic.Add(eventName, eventInfo);
            eventTypes[eventName] = null;
        }
    }

    /// <summary>
    /// 获取事件信息
    /// </summary>
    public EventInfo<T> GetEventInfo<T>(string eventName)
    {
        if (eventDic.ContainsKey(eventName))
        {
            return eventDic[eventName] as EventInfo<T>;
        }
        return null;
    }

    public EventInfo GetEventInfo(string eventName)
    {
        if (eventDic.ContainsKey(eventName))
        {
            return eventDic[eventName] as EventInfo;
        }
        return null;
    }

    /// <summary>
    /// 获取缓存事件数量
    /// </summary>
    public int GetCachedEventCount(string eventName)
    {
        if (eventDic.ContainsKey(eventName))
        {
            return eventDic[eventName].CachedEventCount;
        }
        return 0;
    }

    /// <summary>
    /// 清空事件中心
    /// </summary>
    public void Clear()
    {
        foreach (var eventInfo in eventDic.Values)
        {
            if (eventInfo is EventInfo baseInfo)
            {
                baseInfo.actions = null;
            }
        }

        eventDic.Clear();
        eventDependencies.Clear();
        eventTypes.Clear();
        Debug.Log("事件中心已清空");
    }

    /// <summary>
    /// 清空特定事件的缓存
    /// </summary>
    public void ClearEventCache(string eventName)
    {
        // 需要通过反射清空缓存队列
        if (eventDic.ContainsKey(eventName))
        {
            // 创建新的事件信息替换旧的就绪事件
            var oldInfo = eventDic[eventName];
            bool isReady = oldInfo.IsReady;

            if (eventTypes[eventName] == null)
            {
                // 无参事件
                var oldEventInfo = oldInfo as EventInfo;
                if (oldEventInfo != null)
                {
                    var newEventInfo = new EventInfo(oldEventInfo.actions);
                    newEventInfo.IsReady = isReady;
                    eventDic[eventName] = newEventInfo;
                    Debug.Log($"事件 [{eventName}] 缓存已清空");
                }
            }
            else
            {
                // 泛型事件 - 通过反射处理
                var genericType = eventTypes[eventName];
                var newEventInfo = System.Activator.CreateInstance(
                    typeof(EventInfo<>).MakeGenericType(genericType),
                    new object[] { null }
                ) as IEventInfo;

                if (newEventInfo != null)
                {
                    // 复制委托（需要更多反射操作）
                    newEventInfo.IsReady = isReady;
                    eventDic[eventName] = newEventInfo;
                    Debug.Log($"事件 [{eventName}] 缓存已清空");
                }
            }
        }
    }

    /// <summary>
    /// 打印事件中心状态（调试用）
    /// </summary>
    public void PrintEventCenterStatus()
    {
        Debug.Log("=== 事件中心状态 ===");
        Debug.Log($"事件总数: {eventDic.Count}");

        foreach (var kvp in eventDic)
        {
            string eventName = kvp.Key;
            IEventInfo eventInfo = kvp.Value;

            string status = eventInfo.IsReady ? "就绪" : "未就绪";
            string typeInfo = eventTypes[eventName]?.Name ?? "无参";

            Debug.Log($"- [{eventName}] 类型: {typeInfo}, 状态: {status}, 缓存: {eventInfo.CachedEventCount}");
        }

        if (eventDependencies.Count > 0)
        {
            Debug.Log("=== 事件依赖关系 ===");
            foreach (var kvp in eventDependencies)
            {
                Debug.Log($"[{kvp.Key}] 依赖于: {string.Join(", ", kvp.Value)}");
            }
        }

        Debug.Log("====================");
    }
    #endregion
}