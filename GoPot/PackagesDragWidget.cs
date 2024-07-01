using Godot;
using GoPot;
using System;
using System.Collections.Generic;

[Tool]
public partial class PackagesDragWidget : Control
{
    [Export]
    GoPotEditorWindow EditorWindow;
    [Export]
    Label InfoLabel;

    internal List<PackageWidget> allWidgets;

    public class DragEventArgs
    {
        public PackageWidget DraggedPackage;
        public InputEventMouseButton ButtonEvent;

        public DragEventArgs(PackageWidget draggedPackage, InputEventMouseButton buttonEvent)
        {
            DraggedPackage = draggedPackage;
            ButtonEvent = buttonEvent;
        }
    }

    public event EventHandler<DragEventArgs> OnDragFinished;

    private bool dragStarted = false;

    public void UpdateDrag(PackageWidget packageWidget, InputEventMouseMotion motionEvent)
    {
        if (!dragStarted)
        {
            dragStarted = true;
            Visible = true;

            var allDraggedPackages = PackageWidget.GetPackagesForDrag(allWidgets, packageWidget);
            if (allDraggedPackages.Count > 1)
            {
                // TODO: use format string
                InfoLabel.Text = packageWidget.Package.Name + "\n+" + (allDraggedPackages.Count - 1).ToString() + " more";
            }
            else
            {
                InfoLabel.Text = packageWidget.Package.Name;
            }
        }

        GlobalPosition = motionEvent.GlobalPosition;
    }

    public void EndDrag(PackageWidget packageWidget, InputEventMouseButton buttonEvent)
    {
        dragStarted = false;
        Visible = false;
        OnDragFinished?.Invoke(this, new DragEventArgs(packageWidget, buttonEvent));
    }
}
