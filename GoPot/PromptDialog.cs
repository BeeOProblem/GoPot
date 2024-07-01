using Godot;
using System;

namespace GoPot
{
    [Tool]
    public partial class PromptDialog : Window
    {
        [Export]
        LineEdit TextInput;

        public event EventHandler ClosedOk;
        public event EventHandler ClosedCancelled;

        public string Text
        {
            get
            {
                return TextInput.Text;
            }

            set
            {
                TextInput.Text = value;
            }
        }

        private void OnOkClicked()
        {
            Hide();
            ClosedOk?.Invoke(this, EventArgs.Empty);
        }

        private void OnCancelClicked()
        {
            Hide();
            ClosedCancelled?.Invoke(this, EventArgs.Empty);
        }
    }
}