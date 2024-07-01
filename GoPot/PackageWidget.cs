using Godot;
using System;
using System.Collections.Generic;

namespace GoPot
{
    [Tool]
    public partial class PackageWidget : Control
    {
        [Export]
        Label PackageName;
        [Export]
        CheckBox SelectedCheckbox;
        [Export]
        ColorRect BackgroundColor;
        [Export]
        Button EditManifestButton;

        // TODO: these should be queried from the editor theme
        [Export]
        Color DefaultColor;
        [Export]
        Color SelectedColor;

        public PackagesDragWidget DragWidget;

        public event EventHandler OnEditManifest;

        private PackageInfo package;
        private bool mouseDown;

        public PackageInfo Package
        {
            get
            {
                return package;
            }
        }

        public bool IsSelected
        {
            get
            {
                return SelectedCheckbox.ButtonPressed;
            }

            set
            {
                SelectedCheckbox.ButtonPressed = value;
            }
        }

        internal static List<PackageWidget> GetPackagesForDrag(List<PackageWidget> allWidgets, PackageWidget dragOriginPackage)
        {
            List<PackageWidget> output = new List<PackageWidget>();
            bool useInUsePackages = dragOriginPackage.Package.IsInUse;
            foreach (PackageWidget widget in allWidgets)
            {
                // need to check if widget == dragOriginPackage in case the dragged package is unchecked
                // since check/uncheck happens after the mouse is released but dragging happens before then
                if (widget.IsSelected && widget.GetParent() == dragOriginPackage.GetParent() || widget == dragOriginPackage)
                {
                    output.Add(widget);
                }
            }

            return output;
        }

        public void Init(PackageInfo package, bool allowManifestEdit)
        {
            this.package = package;
            EditManifestButton.Visible = allowManifestEdit;
        }

        public void Refresh()
        {
            PackageName.Text = package.Name;
        }

        public void OnToggled(bool newSelected)
        {
            if (newSelected)
            {
                BackgroundColor.Color = SelectedColor;
            }
            else
            {
                BackgroundColor.Color = DefaultColor;
            }
        }

        public override void _Ready()
        {
            Refresh();
        }

        public override void _GuiInput(InputEvent inputEvent)
        {
            if (inputEvent is InputEventMouseButton buttonEvent)
            {
                if (buttonEvent.ButtonIndex == MouseButton.Left)
                {
                    if (buttonEvent.Pressed)
                    {
                        mouseDown = true;
                    }
                    else if (mouseDown)
                    {
                        mouseDown = false;
                        if (buttonEvent.Position.X >= 0 && buttonEvent.Position.Y >= 0 &&
                           buttonEvent.Position.X < Size.X && buttonEvent.Position.Y < Size.Y)
                        {
                            SelectedCheckbox.ButtonPressed = !SelectedCheckbox.ButtonPressed;
                        }

                        DragWidget.EndDrag(this, buttonEvent);
                    }
                }
            }
            else if(inputEvent is InputEventMouseMotion motionEvent)
            {
                // TODO: have threshold before movement counts as a "drag"
                if(mouseDown)
                {
                    DragWidget.UpdateDrag(this, motionEvent);
                }
            }
        }

        private void OnEditManifestClicked()
        {
            OnEditManifest.Invoke(this, EventArgs.Empty);
        }
    }
}