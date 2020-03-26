using ExcelDna.Integration;

namespace Qwack.Excel
{
    public class StartUp : IExcelAddIn
    {
        public void AutoClose()
        {
            
        }

        public void AutoOpen()
        {
            Utils.ExcelUtils.QUtils_WarmUp();
        }
    }
}
