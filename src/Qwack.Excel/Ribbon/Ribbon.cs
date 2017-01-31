using System.Runtime.InteropServices;
using ExcelDna.Integration.CustomUI;


namespace Qwack.Excel.Ribbon
{
    [ComVisible(true)]
    public class MyRibbon : ExcelRibbon
    {
        public void SayHello(IRibbonControl control1)
        {
            //MessageBox.Show("Hello!");
        }
    }

}
