namespace ModelTest.MeterTest;

/// <summary>
/// 将运行期工位结论、电表档案和协议数值合并成一次可持久化的测试任务快照。
/// 构建器不依赖 WinForms，便于独立模拟完整、失败和部分测试场景。
/// </summary>
public static class MeterTestResultSnapshotBuilder
{
    /// <summary>合并方案上下文、工位状态、资产档案和测量值，生成可持久化的完整任务快照。</summary>
    public static MeterTestResultTaskSnapshot Build(
        string runId,
        string schemeName,
        DateTime startedAt,
        DateTime endedAt,
        string status,
        string saveMode,
        IEnumerable<int> stationNumbers,
        IReadOnlyDictionary<int, MeterArchiveData> archives,
        IEnumerable<MeterTestStoredStationResultData> storedResults,
        IEnumerable<MeterTestMeasurementData> measurements,
        IEnumerable<(string TestItemName, string TestSubItemName)>? expectedTests = null)
    {
        HashSet<int> stationSet = stationNumbers.Where(number => number > 0).ToHashSet();
        List<MeterTestStoredStationResultData> relevantResults = storedResults
            .Where(result => stationSet.Contains(result.StationNo))
            .Where(result => string.IsNullOrWhiteSpace(schemeName) ||
                             result.SchemeName.Equals(schemeName, StringComparison.OrdinalIgnoreCase))
            .GroupBy(
                result => CreateResultKey(
                    result.StationNo,
                    result.TestItemName,
                    result.TestSubItemName),
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToList();
        List<MeterTestMeasurementData> relevantMeasurements = measurements
            .Where(result => stationSet.Contains(result.StationNo))
            .GroupBy(
                measurement => CreateMeasurementKey(measurement),
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToList();
        HashSet<string> expectedTestKeys = (expectedTests ?? Array.Empty<(string, string)>())
            .Where(test => !string.IsNullOrWhiteSpace(test.TestItemName) &&
                           !string.IsNullOrWhiteSpace(test.TestSubItemName))
            .Select(test => CreateTestKey(test.TestItemName, test.TestSubItemName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<MeterTestResultDetailData> details = relevantResults
            .Select(result => new MeterTestResultDetailData(
                result.StationNo,
                result.TestItemName,
                result.TestSubItemName,
                NormalizeResult(result.State.Result),
                result.State.Time,
                FirstNonEmpty(result.State.Message, result.State.ToolTip),
                string.Empty,
                0,
                string.Empty,
                null,
                string.Empty,
                null,
                string.Empty))
            .ToList();

        foreach (MeterTestMeasurementData measurement in relevantMeasurements)
        {
            MeterTestStoredStationResultData? matchingResult = relevantResults.LastOrDefault(result =>
                result.StationNo == measurement.StationNo &&
                result.TestItemName.Equals(measurement.TestItemName, StringComparison.OrdinalIgnoreCase) &&
                result.TestSubItemName.Equals(measurement.TestSubItemName, StringComparison.OrdinalIgnoreCase));
            matchingResult ??= relevantResults.LastOrDefault(result =>
                result.StationNo == measurement.StationNo &&
                result.TestItemName.Equals(measurement.TestItemName, StringComparison.OrdinalIgnoreCase));

            details.Add(new MeterTestResultDetailData(
                measurement.StationNo,
                measurement.TestItemName,
                measurement.TestSubItemName,
                NormalizeResult(matchingResult?.State.Result),
                matchingResult?.State.Time ?? string.Empty,
                FirstNonEmpty(matchingResult?.State.Message, matchingResult?.State.ToolTip),
                measurement.MeasurementName,
                measurement.SequenceNo,
                measurement.ValueText,
                measurement.NumericValue,
                measurement.Unit,
                measurement.AverageValue,
                measurement.LimitText));
        }

        // 任务明细的唯一键与数据库唯一索引保持一致。
        // 结论行 MeasurementName 为空、SequenceNo 为0；各数值行按测量名称和轮次独立保留。
        details = details
            .GroupBy(CreateDetailKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .OrderBy(detail => detail.StationNo)
            .ThenBy(detail => detail.TestItemName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(detail => detail.TestSubItemName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(detail => detail.MeasurementName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(detail => detail.SequenceNo)
            .ToList();

        List<MeterTestResultStationData> stations = new();
        foreach (int stationNo in stationSet.OrderBy(number => number))
        {
            MeterArchiveData archive = archives.TryGetValue(stationNo, out MeterArchiveData? resolvedArchive)
                ? resolvedArchive
                : CreateEmptyArchive(stationNo);
            List<string> stationConclusions = relevantResults
                .Where(result => result.StationNo == stationNo)
                .Select(result => NormalizeResult(result.State.Result))
                .Where(result => !string.IsNullOrWhiteSpace(result))
                .ToList();
            if (expectedTestKeys.Count > 0)
            {
                HashSet<string> completedTestKeys = relevantResults
                    .Where(result => result.StationNo == stationNo)
                    .Select(result => CreateTestKey(result.TestItemName, result.TestSubItemName))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (!expectedTestKeys.IsSubsetOf(completedTestKeys))
                {
                    stationConclusions.Add("未完成");
                }
            }
            string overallResult = ResolveOverallResult(stationConclusions);
            stations.Add(new MeterTestResultStationData(
                stationNo,
                archive.Barcode ?? string.Empty,
                archive.MeterAddress ?? string.Empty,
                archive.MeterType ?? string.Empty,
                archive.AccessMode ?? string.Empty,
                archive.Voltage ?? string.Empty,
                archive.Current ?? string.Empty,
                archive.CurrentSpecification ?? string.Empty,
                archive.ActiveClass ?? string.Empty,
                archive.ActiveConstant ?? string.Empty,
                archive.ReactiveClass ?? string.Empty,
                archive.ReactiveConstant ?? string.Empty,
                overallResult,
                endedAt));
        }

        int passedCount = stations.Count(station => station.OverallResult == "合格");
        int failedCount = stations.Count(station => station.OverallResult == "不合格");
        int incompleteCount = stations.Count - passedCount - failedCount;
        string summary = $"工位数={stations.Count}，合格={passedCount}，不合格={failedCount}，未完成={incompleteCount}";
        return new MeterTestResultTaskSnapshot(
            string.IsNullOrWhiteSpace(runId) ? Guid.NewGuid().ToString("N") : runId,
            schemeName ?? string.Empty,
            startedAt,
            endedAt,
            status ?? string.Empty,
            saveMode ?? string.Empty,
            summary,
            stations,
            details);
    }

    /// <summary>根据全部小项结论计算任务或工位的合格、不合格、未完成汇总结论。</summary>
    private static string ResolveOverallResult(IReadOnlyCollection<string> conclusions)
    {
        if (conclusions.Count == 0)
            return "未完成";
        if (conclusions.Any(result => result == "不合格"))
            return "不合格";
        return conclusions.All(result => result == "合格") ? "合格" : "未完成";
    }

    /// <summary>将界面和服务返回的多种结论文本统一为数据库标准状态。</summary>
    private static string NormalizeResult(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized switch
        {
            "合格" => "合格",
            "不合格" => "不合格",
            "测试中" or "待测试" or "" => "未完成",
            _ => normalized
        };
    }

    /// <summary>返回两个候选文本中第一个非空值。</summary>
    private static string FirstNonEmpty(string? first, string? second)
    {
        return !string.IsNullOrWhiteSpace(first) ? first : second ?? string.Empty;
    }

    /// <summary>为测试项和测试小项构造测量数据索引键。</summary>
    private static string CreateTestKey(string testItemName, string testSubItemName)
    {
        return $"{testItemName.Trim()}\u001f{testSubItemName.Trim()}";
    }

    /// <summary>为工位、测试项和小项构造界面结论索引键。</summary>
    private static string CreateResultKey(int stationNo, string testItemName, string testSubItemName)
    {
        return $"{stationNo}\u001f{CreateTestKey(testItemName, testSubItemName)}";
    }

    /// <summary>从测量记录生成与结果明细一致的唯一索引键。</summary>
    private static string CreateMeasurementKey(MeterTestMeasurementData measurement)
    {
        return $"{CreateResultKey(measurement.StationNo, measurement.TestItemName, measurement.TestSubItemName)}"
            + $"\u001f{measurement.MeasurementName.Trim()}\u001f{measurement.SequenceNo}";
    }

    /// <summary>从结果明细生成用于去重和合并的唯一索引键。</summary>
    private static string CreateDetailKey(MeterTestResultDetailData detail)
    {
        return $"{CreateResultKey(detail.StationNo, detail.TestItemName, detail.TestSubItemName)}"
            + $"\u001f{detail.MeasurementName.Trim()}\u001f{detail.SequenceNo}";
    }

    /// <summary>为资产缺失工位创建字段完整的空档案，保证部分测试也能安全保存。</summary>
    private static MeterArchiveData CreateEmptyArchive(int stationNo)
    {
        return new MeterArchiveData(
            stationNo,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);
    }
}
