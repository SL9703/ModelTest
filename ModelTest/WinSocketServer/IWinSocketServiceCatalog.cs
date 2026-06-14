using System.Collections.Generic;

namespace ModelTest
{
    public interface IWinSocketServiceCatalog
    {
        IReadOnlyList<string> GetServiceNames();
    }
}
