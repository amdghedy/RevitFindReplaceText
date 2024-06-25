using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Windows;

namespace FindReplaceRevit
{
    public partial class ReplaceTextWindow : Window
    {
        private Document _doc;

        public ReplaceTextWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;
        }

        private void btnReplace_Click(object sender, RoutedEventArgs e)
        {
            string findText = txtFindText.Text;
            string replaceText = txtReplaceText.Text;
            bool preview = chkPreview.IsChecked ?? false;

            if (string.IsNullOrEmpty(findText) || string.IsNullOrEmpty(replaceText))
            {
                MessageBox.Show("Please enter both 'Find Text' and 'Replace With' values.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ReplaceTextInProject(_doc, findText, replaceText, preview);
            MessageBox.Show("Text replacement completed.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ReplaceTextInProject(Document doc, string findText, string replaceText, bool preview)
        {
            List<string> log = new List<string>();
            bool textFound = false;

            using (Transaction tx = new Transaction(doc, "Replace Text"))
            {
                tx.Start();

                try
                {
                    FilteredElementCollector collector = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType();

                    foreach (View view in collector.OfClass(typeof(View)))
                    {
                        if (!IsValidView(view))
                            continue;

                        bool textFoundInView = ReplaceTextInView(doc, view, findText, replaceText, log);
                        if (textFoundInView)
                        {
                            textFound = true;
                            log.Add($"Found '{findText}' in view '{view.Name}'");
                        }
                    }

                    if (preview)
                    {
                        MessageBox.Show(string.Join("\n", log), "Preview of Changes", MessageBoxButton.OK, MessageBoxImage.Information);
                        tx.RollBack();
                    }
                    else
                    {
                        if (textFound)
                        {
                            tx.Commit();
                            MessageBox.Show(string.Join("\n", log), "Changes Made", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            tx.RollBack();
                            MessageBox.Show("No matches found for the given text.", "Replace Text", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    tx.RollBack();
                }
            }
        }

        private bool IsValidView(View view)
        {
            return view != null && view.IsValidObject;
        }

        private bool ReplaceTextInView(Document doc, View view, string findText, string replaceText, List<string> log)
        {
            bool textFound = false;

            if (!IsValidView(view))
            {
                log.Add($"View '{view?.Name}' is not valid or does not exist.");
                return false;
            }

            try
            {
                // Replace text in TextNotes
                FilteredElementCollector textNoteCollector = new FilteredElementCollector(doc, view.Id)
                    .OfClass(typeof(TextNote));

                foreach (TextNote textNote in textNoteCollector)
                {
                    if (textNote.Text.Contains(findText))
                    {
                        textNote.Text = textNote.Text.Replace(findText, replaceText);
                        log.Add($"Replaced '{findText}' with '{replaceText}' in TextNote ID {textNote.Id}");
                        textFound = true;
                    }
                }

                // Replace text in Tags (IndependentTags)
                FilteredElementCollector tagCollector = new FilteredElementCollector(doc, view.Id)
                    .OfClass(typeof(IndependentTag));

                foreach (IndependentTag tag in tagCollector)
                {
                    Parameter param = tag.LookupParameter("Comments");
                    if (param != null && !param.IsReadOnly && param.HasValue)
                    {
                        string tagText = param.AsString();
                        if (!string.IsNullOrEmpty(tagText) && tagText.Contains(findText))
                        {
                            param.Set(tagText.Replace(findText, replaceText));
                            log.Add($"Replaced '{findText}' with '{replaceText}' in Tag ID {tag.Id}");
                            textFound = true;
                        }
                    }
                }

                // Replace text in Labels (FamilyInstances with label parameters)
                FilteredElementCollector familyInstanceCollector = new FilteredElementCollector(doc, view.Id)
                    .OfClass(typeof(FamilyInstance));

                foreach (FamilyInstance familyInstance in familyInstanceCollector)
                {
                    foreach (Parameter param in familyInstance.Parameters)
                    {
                        if (param.StorageType == StorageType.String && !param.IsReadOnly && param.HasValue)
                        {
                            string paramText = param.AsString();
                            if (!string.IsNullOrEmpty(paramText) && paramText.Contains(findText))
                            {
                                param.Set(paramText.Replace(findText, replaceText));
                                log.Add($"Replaced '{findText}' with '{replaceText}' in Parameter '{param.Definition.Name}' of FamilyInstance ID {familyInstance.Id}");
                                textFound = true;
                            }
                        }
                    }
                }

                return textFound;
            }
            catch (System.Exception ex)
            {
                log.Add($"Error replacing text in view '{view.Name}': {ex.Message}");
                return false;
            }
        }
    }
}
