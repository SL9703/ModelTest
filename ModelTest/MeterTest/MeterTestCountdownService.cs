namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 统一倒计时服务。
/// 日计时、起动、潜动和基本误差共用该服务，每秒只发布界面状态，不逐秒写测试日志。
/// </summary>
public sealed class MeterTestCountdownService
{
    private readonly object syncRoot = new();
    private readonly Dictionary<Guid, CountdownEntry> activeCountdowns = new();

    /// <summary>倒计时显示状态变化事件；调用方负责切换到UI线程。</summary>
    public event Action<MeterTestCountdownState>? StateChanged;

    /// <summary>
    /// 等待指定秒数并每秒发布一次剩余时间。
    /// 多个控制PCB并行执行同一个测试小项时按标题合并，只展示该TestSubItem的一条倒计时。
    /// </summary>
    public async Task DelayAsync(
        int waitSeconds,
        string title,
        CancellationToken cancellationToken)
    {
        int normalizedSeconds = Math.Max(0, waitSeconds);
        if (normalizedSeconds == 0)
            return;

        Guid countdownId = Guid.NewGuid();
        string normalizedTitle = string.IsNullOrWhiteSpace(title) ? "测试等待" : title.Trim();
        UpdateEntry(countdownId, normalizedTitle, normalizedSeconds);
        try
        {
            for (int remainingSeconds = normalizedSeconds; remainingSeconds > 0; remainingSeconds--)
            {
                cancellationToken.ThrowIfCancellationRequested();
                UpdateEntry(countdownId, normalizedTitle, remainingSeconds);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            RemoveEntry(countdownId);
        }
    }

    /// <summary>新增或更新一个活动倒计时，然后发布聚合状态。</summary>
    private void UpdateEntry(Guid countdownId, string title, int remainingSeconds)
    {
        MeterTestCountdownState state;
        lock (syncRoot)
        {
            activeCountdowns[countdownId] = new CountdownEntry(title, remainingSeconds);
            state = CreateCurrentState();
        }

        PublishState(state);
    }

    /// <summary>倒计时结束或取消时移除对应任务，并恢复剩余任务或空闲状态。</summary>
    private void RemoveEntry(Guid countdownId)
    {
        MeterTestCountdownState state;
        lock (syncRoot)
        {
            activeCountdowns.Remove(countdownId);
            state = CreateCurrentState();
        }

        PublishState(state);
    }

    /// <summary>在锁内生成当前需要展示的倒计时状态。</summary>
    private MeterTestCountdownState CreateCurrentState()
    {
        if (activeCountdowns.Count == 0)
            return MeterTestCountdownState.Idle;

        CountdownEntry displayEntry = activeCountdowns.Values
            .GroupBy(entry => entry.Title, StringComparer.Ordinal)
            .Select(group => new CountdownEntry(
                group.Key,
                group.Max(entry => entry.RemainingSeconds)))
            .OrderByDescending(entry => entry.RemainingSeconds)
            .First();
        return new MeterTestCountdownState(true, displayEntry.Title, displayEntry.RemainingSeconds);
    }

    /// <summary>安全发布状态，避免界面订阅异常中断测试等待流程。</summary>
    private void PublishState(MeterTestCountdownState state)
    {
        try
        {
            StateChanged?.Invoke(state);
        }
        catch (Exception ex)
        {
            LogMessage.Error("[MeterTest倒计时] 界面状态更新失败。", ex);
        }
    }

    /// <summary>一个并发测试流程在界面倒计时区域中的标题和剩余秒数。</summary>
    private sealed record CountdownEntry(string Title, int RemainingSeconds);
}

/// <summary>测试过程区域使用的倒计时显示状态。</summary>
public sealed record MeterTestCountdownState(bool IsActive, string Title, int RemainingSeconds)
{
    /// <summary>没有测试等待任务时的默认状态。</summary>
    public static MeterTestCountdownState Idle { get; } = new(false, "倒计时", 0);
}
