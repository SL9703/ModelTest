using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ModelTest.MeterTest;

/// <summary>
/// 通用的一发一收测试执行器。
/// 当前主窗体主要使用更细的多工位并发流程；该类保留给单项串行执行和后续复用。
/// </summary>
public sealed class MeterTestExecutor
{
    /// <summary>
    /// 实际的一发一收委托由 UI 或通信层注入，执行器只负责超时、匹配和结果封装。
    /// </summary>
    public Func<MeterTestSubItem, CancellationToken, Task<string?>>? SendAndReceiveAsync { get; set; }

    /// <summary>
    /// 执行一个完整方案下的全部测试项。
    /// </summary>
    public async Task<List<MeterTestExecutionResult>> ExecuteSchemeAsync(
        MeterTestScheme scheme,
        CancellationToken cancellationToken)
    {
        List<MeterTestExecutionResult> results = new();

        foreach (MeterTestItem testItem in scheme.TestItems)
        {
            results.AddRange(await ExecuteItemAsync(scheme.Name, testItem, cancellationToken));
        }

        return results;
    }

    /// <summary>
    /// 执行一个测试项下的全部测试小项。
    /// </summary>
    public async Task<List<MeterTestExecutionResult>> ExecuteItemAsync(
        string schemeName,
        MeterTestItem testItem,
        CancellationToken cancellationToken)
    {
        List<MeterTestExecutionResult> results = new();

        foreach (MeterTestSubItem subItem in testItem.TestSubItems)
        {
            results.Add(await ExecuteSubItemAsync(schemeName, testItem.Name, subItem, cancellationToken));
        }

        return results;
    }

    /// <summary>
    /// 执行单个测试小项，并根据配置的匹配规则给出合格/不合格结论。
    /// </summary>
    public async Task<MeterTestExecutionResult> ExecuteSubItemAsync(
        string schemeName,
        string testItemName,
        MeterTestSubItem subItem,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        DateTime startedAt = DateTime.Now;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (SendAndReceiveAsync is null)
            {
                return MeterTestExecutionResult.Fail(
                    schemeName,
                    testItemName,
                    subItem.Name,
                    startedAt,
                    stopwatch.ElapsedMilliseconds,
                    "未配置一发一收执行方法。");
            }

            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(Math.Max(100, subItem.TimeoutMs));

            string? response = await SendAndReceiveAsync(subItem, timeoutCts.Token);
            bool passed = IsResponseMatched(subItem, response);
            string message = passed
                ? "应答匹配，测试通过。"
                : $"应答不匹配，期望：{subItem.ExpectedResponse}，实际：{response ?? "空"}";

            return new MeterTestExecutionResult(
                schemeName,
                testItemName,
                subItem.Name,
                passed,
                response ?? string.Empty,
                message,
                startedAt,
                stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return MeterTestExecutionResult.Fail(
                schemeName,
                testItemName,
                subItem.Name,
                startedAt,
                stopwatch.ElapsedMilliseconds,
                $"等待超时，超时时间 {subItem.TimeoutMs} ms。");
        }
        catch (OperationCanceledException)
        {
            return MeterTestExecutionResult.Fail(
                schemeName,
                testItemName,
                subItem.Name,
                startedAt,
                stopwatch.ElapsedMilliseconds,
                "测试已取消。");
        }
        catch (Exception ex)
        {
            return MeterTestExecutionResult.Fail(
                schemeName,
                testItemName,
                subItem.Name,
                startedAt,
                stopwatch.ElapsedMilliseconds,
                $"执行异常：{ex.Message}");
        }
    }

    /// <summary>
    /// 普通 HEX 响应匹配。
    /// 如果没有配置期望值，则只要有响应就认为通过。
    /// </summary>
    private static bool IsResponseMatched(MeterTestSubItem subItem, string? response)
    {
        string normalizedResponse = Normalize(response);
        string normalizedExpected = Normalize(subItem.ExpectedResponse);

        if (string.IsNullOrEmpty(normalizedExpected))
        {
            return !string.IsNullOrEmpty(normalizedResponse);
        }

        ResponseMatchMode matchMode = ParseMatchMode(subItem.MatchMode);

        return matchMode switch
        {
            ResponseMatchMode.Exact => normalizedResponse.Equals(normalizedExpected, StringComparison.OrdinalIgnoreCase),
            ResponseMatchMode.StartsWith => normalizedResponse.StartsWith(normalizedExpected, StringComparison.OrdinalIgnoreCase),
            _ => normalizedResponse.Contains(normalizedExpected, StringComparison.OrdinalIgnoreCase)
        };
    }

    /// <summary>
    /// 将 XML 中的匹配模式字符串转换为枚举；非法配置默认按 Contains 处理。
    /// </summary>
    private static ResponseMatchMode ParseMatchMode(string? value)
    {
        return Enum.TryParse(value, true, out ResponseMatchMode mode)
            ? mode
            : ResponseMatchMode.Contains;
    }

    /// <summary>
    /// 去除空格，方便 HEX 字符串做大小写无关比较。
    /// </summary>
    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Replace(" ", string.Empty).Trim();
    }
}

/// <summary>
/// 单个测试小项的执行结果。
/// </summary>
public sealed record MeterTestExecutionResult(
    string SchemeName,
    string TestItemName,
    string TestSubItemName,
    bool Passed,
    string Response,
    string Message,
    DateTime StartedAt,
    long ElapsedMilliseconds)
{
    /// <summary>
    /// 快速创建失败结果，统一失败返回结构。
    /// </summary>
    public static MeterTestExecutionResult Fail(
        string schemeName,
        string testItemName,
        string testSubItemName,
        DateTime startedAt,
        long elapsedMilliseconds,
        string message)
    {
        return new MeterTestExecutionResult(
            schemeName,
            testItemName,
            testSubItemName,
            false,
            string.Empty,
            message,
            startedAt,
            elapsedMilliseconds);
    }
}
