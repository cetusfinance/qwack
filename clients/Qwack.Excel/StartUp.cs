using ExcelDna.Integration;

namespace Qwack.Excel
{
    public class StartUp : IExcelAddIn
    {
        public void AutoClose()
        {
            Qwack.Utils.Parallel.ParallelUtils.Instance.Dispose();
        }

        public void AutoOpen()
        {
            Utils.ExcelUtils.QUtils_WarmUp();
        }
    }
}
