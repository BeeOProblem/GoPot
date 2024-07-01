using Godot;
using System;
using System.Collections.Generic;

namespace GoPot
{
    [Tool]
    public partial class GoPotManifestWindow : Window
    {
        [Export]
        Label PackageDirectoryName;
        [Export]
        LineEdit PackageName;
        [Export]
        PackedScene PackageWidgetScene;
        [Export]
        PackagesDragWidget DragWidget;

        [Export]
        Container AvailablePackagesContainer;
        [Export]
        Control AvailablePackagesDragTarget;
        [Export]
        Label AvailablePackagesEmptyLabel;
        [Export]
        Container DependencyPackagesContainer;
        [Export]
        Control DependencyPackagesDragTarget;
        [Export]
        Label DependencyPackagesEmptyLabel;

        public event EventHandler<EventArgs> OnSavedChanges;

        private PackageManager packageManager;
        private PackageInfo packageToEdit;
        private List<PackageWidget> allWidgets;
        private List<PackageInfo> orignalDependencies;

        internal void ShowForPackage(PackageManager packageManager, PackageInfo packageToEdit)
        {
            allWidgets = new List<PackageWidget>();
            DragWidget.allWidgets = allWidgets;

            this.packageToEdit = packageToEdit;
            this.packageManager = packageManager;
            PackageDirectoryName.Text = packageToEdit.DirectoryName;
            PackageName.Text = packageToEdit.Name;

            // NOTE: first child is a stub label for when the list and shouldn't be deleted
            for (int i = 1; i < DependencyPackagesContainer.GetChildCount(); i++)
            {
                DependencyPackagesContainer.GetChild(i).QueueFree();
            }

            // NOTE: first child is a stub label for when the list and shouldn't be deleted
            for (int i = 1; i < AvailablePackagesContainer.GetChildCount(); i++)
            {
                AvailablePackagesContainer.GetChild(i).QueueFree();
            }

            bool hasDependencies = false;
            bool haveAvailablePackages = false;
            orignalDependencies = new List<PackageInfo>();
            foreach (PackageInfo package in packageManager.AllPackages)
            {
                if (package == packageToEdit)
                {
                    continue;
                }

                PackageWidget packageWidget = PackageWidgetScene.Instantiate<PackageWidget>();
                packageWidget.Init(package, false);
                packageWidget.DragWidget = DragWidget;
                allWidgets.Add(packageWidget);

                if (packageToEdit.Dependencies.Contains(package))
                {
                    orignalDependencies.Add(package);
                    DependencyPackagesContainer.AddChild(packageWidget);
                    hasDependencies = true;
                }
                else
                {
                    AvailablePackagesContainer.AddChild(packageWidget);
                    haveAvailablePackages = true;
                }
            }


            AvailablePackagesEmptyLabel.Visible = !haveAvailablePackages;
            DependencyPackagesEmptyLabel.Visible = !hasDependencies;

            Show();
        }

        public override void _Ready()
        {
            DragWidget.OnDragFinished += _OnDragFinished;
        }

        private void _OnDragFinished(object sender, PackagesDragWidget.DragEventArgs e)
        {
            FinishDragSelectedPackages(e.DraggedPackage, e.ButtonEvent);
        }

        private void OnOkClicked()
        {
            packageToEdit.Name = PackageName.Text;
            packageToEdit.SaveManifest();
            OnSavedChanges.Invoke(this, EventArgs.Empty);
            Hide();
        }

        private void OnCancelClicked()
        {
            packageToEdit.Dependencies = orignalDependencies;
            Hide();
        }

        private void OnAddClicked()
        {
            foreach (PackageWidget widget in allWidgets)
            {
                if (widget.IsSelected)
                {
                    if (!AddDependencyWithWidget(widget)) break;
                }
            }
        }

        private void OnRemoveClicked()
        {
            foreach (PackageWidget widget in allWidgets)
            {
                if (widget.IsSelected)
                {
                    if (!RemoveDependencyWithWidget(widget)) break;
                }
            }
        }

        private void FinishDragSelectedPackages(PackageWidget dragOriginPackage, InputEventMouse mouseEvent)
        {
            var movedPackages = PackageWidget.GetPackagesForDrag(allWidgets, dragOriginPackage);
            if (AvailablePackagesDragTarget.GetGlobalRect().HasPoint(mouseEvent.GlobalPosition))
            {
                GD.Print("Dragging packages out of dependency list");
                if (packageToEdit.Dependencies.Contains(dragOriginPackage.Package))
                {
                    foreach (PackageWidget widget in movedPackages)
                    {
                        if (!RemoveDependencyWithWidget(widget)) break;
                    }
                }
            }
            else if (DependencyPackagesDragTarget.GetGlobalRect().HasPoint(mouseEvent.GlobalPosition))
            {
                GD.Print("Dragging packages into dependency list");
                if (!packageToEdit.Dependencies.Contains(dragOriginPackage.Package))
                {
                    foreach (PackageWidget widget in movedPackages)
                    {
                        if (!AddDependencyWithWidget(widget)) break;
                    }
                }
            }
        }

        private bool AddDependencyWithWidget(PackageWidget widget)
        {
            // TODO: detect circular dependency attempt and block
            packageToEdit.Dependencies.Add(widget.Package);

            widget.IsSelected = false;
            widget.Reparent(DependencyPackagesContainer, false);
            DependencyPackagesEmptyLabel.Visible = false;
            AvailablePackagesEmptyLabel.Visible = AvailablePackagesContainer.GetChildCount() == 1;
            return true;
        }

        private bool RemoveDependencyWithWidget(PackageWidget widget)
        {
            packageToEdit.Dependencies.Remove(widget.Package);

            widget.IsSelected = false;
            widget.Reparent(AvailablePackagesContainer, false);
            AvailablePackagesEmptyLabel.Visible = false;
            DependencyPackagesEmptyLabel.Visible = DependencyPackagesContainer.GetChildCount() == 1;
            return true;
        }
    }
}
