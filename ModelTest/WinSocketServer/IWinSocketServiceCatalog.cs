using System.Collections.Generic;

namespace ModelTest
{
    /// <summary>
    /// WinSocket 接口目录抽象。
    /// 用于向 UI 提供可搜索、可展示的接口清单。
    /// </summary>
    public interface IWinSocketServiceCatalog
    {
        IReadOnlyList<string> GetServiceNames();
    }
}
