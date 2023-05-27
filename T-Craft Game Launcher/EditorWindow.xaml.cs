using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace T_Craft_Game_Launcher
{
    public partial class EditorWindow : Window
    {
        // List of keywords to highlight
        private List<string> keywords = new List<string> { "break", "case", "catch", "continue", "debugger", "default", "delete", "do", "else", "finally", "for", "function", "if", "in", "instanceof", "new", "return", "switch", "this", "throw", "try", "typeof", "var", "void", "while", "with" };

        public EditorWindow()
        {
            InitializeComponent();
        }

        private void codeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        private void codeBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Get the RichTextBox
            RichTextBox rtb = sender as RichTextBox;
            // Append the entered text to the RichTextBox
            rtb.AppendText(e.Text);
        }

        private void codeBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Check if the pressed key is Backspace
            if (e.Key == Key.Back)
            {
                // Get the RichTextBox
                RichTextBox rtb = sender as RichTextBox;
                // Get the current caret position
                TextPointer caretPos = rtb.CaretPosition;
                // Check if the caret is not at the start of the document
                if (caretPos.CompareTo(rtb.Document.ContentStart) > 0)
                {
                    // Move the caret back by one symbol
                    caretPos = caretPos.GetPositionAtOffset(-1);
                    // Delete the symbol before the caret
                    rtb.Document.ContentStart.DeleteTextInRun(1);
                    // Set the new caret position
                    rtb.CaretPosition = caretPos;
                }
            }
        }

    }
}
