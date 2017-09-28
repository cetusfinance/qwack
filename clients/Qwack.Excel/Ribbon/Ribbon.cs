using System.Runtime.InteropServices;
using ExcelDna.Integration.CustomUI;
using ExcelDna.Integration;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System;
using MSExcel = Microsoft.Office.Interop.Excel;


namespace Qwack.Excel.Ribbon
{
    [ComVisible(true)]
    public class QwackRibbon : ExcelRibbon
    {
        private static CustomTaskPane _calViewer;

        public static CustomTaskPane CalViewer { get => _calViewer; set => _calViewer = value; }

        public void DisplayAbout(IRibbonControl control1)
        {
            var assembly = Assembly.GetAssembly(typeof(QwackRibbon));
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var versionString = fvi.FileVersion;
            MessageBox.Show($"Qwack version {versionString} \n © Qwack 2017");
        }

        public static void ShowCalendarViewer()
        {
            if (CalViewer == null)
            {
                // Make a new one using ExcelDna.Integration.CustomUI.CustomTaskPaneFactory 
                //_calViewer = CustomTaskPaneFactory.CreateCustomTaskPane(typeof(MyUserControl), "My Super Task Pane");
                CalViewer.Visible = true;
                CalViewer.DockPosition = MsoCTPDockPosition.msoCTPDockPositionLeft;
                try
                {

                    var xlApp = (MSExcel.Application)ExcelDnaUtil.Application;
                    var xlWb = xlApp.ActiveWorkbook;
                    //  xlWb.SheetSelectionChange += new MSExcel.WorkbookEvents_SheetSelectionChangeEventHandler(xlWorkBook_SheetSelectionChange);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            }
            else
            {
                // Just show it again
                CalViewer.Visible = true;
            }
        }
    }

}
