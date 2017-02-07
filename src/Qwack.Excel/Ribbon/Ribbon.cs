using System.Runtime.InteropServices;
using ExcelDna.Integration.CustomUI;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;

namespace Qwack.Excel.Ribbon
{
    [ComVisible(true)]
    public class QwackRibbon : ExcelRibbon
    {
        public void DisplayAbout(IRibbonControl control1)
        {
            Assembly assembly = Assembly.GetAssembly(typeof(QwackRibbon));
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var versionString = fvi.FileVersion;
            MessageBox.Show($"Qwack version {versionString} \n © Qwack 2017");
        }
    }

}
