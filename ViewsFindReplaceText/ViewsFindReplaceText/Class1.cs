using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Windows;

namespace FindReplaceRevit
{
    [Transaction(TransactionMode.Manual)]
    public class ReplaceTextCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            // Open the WPF window
            ReplaceTextWindow replaceWindow = new ReplaceTextWindow(doc);
            replaceWindow.ShowDialog();

            return Result.Succeeded;
        }
    }
}
