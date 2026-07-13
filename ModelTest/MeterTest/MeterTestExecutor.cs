using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ModelTest.MeterTest;

public sealed class MeterTestExecutor
{
    public Func<MeterTestSubItem, CancellationToken, Task<string?>>? SendAndReceiveAsync { get; set; }

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

    private static ResponseMatchMode ParseMatchMode(string? value)
    {
        return Enum.TryParse(value, true, out ResponseMatchMode mode)
            ? mode
            : ResponseMatchMode.Contains;
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Replace(" ", string.Empty).Trim();
    }
}

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
