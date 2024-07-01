using Godot;
using System;
using System.IO;
using System.Collections.Generic;

namespace GoPot
{
    [Tool]
    public partial class GoPotEditorWindow : Control
    {
        [Export]
        PackedScene ConfigureWindow;
        [Export]
        AcceptDialog AcceptDialog;
        [Export]
        PromptDialog PromptDialog;
        [Export]
        GoPotManifestWindow ManifestDialog;
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
        Container UserPackagesContainer;
        [Export]
        Control UserPackagesDragTarget;
        [Export]
        Label UserPackagesEmptyLabel;

        private PackageManager packageManager;
        private List<PackageWidget> allWidgets;

        internal void FinishDragSelectedPackages(PackageWidget dragOriginPackage, InputEventMouse mouseEvent)
        {
            var movedPackages = PackageWidget.GetPackagesForDrag(allWidgets, dragOriginPackage);
            if (AvailablePackagesDragTarget.GetGlobalRect().HasPoint(mouseEvent.GlobalPosition))
            {
                GD.Print("Dragging packages out of project");
                if (dragOriginPackage.Package.IsInUse)
                {
                    foreach (PackageWidget widget in movedPackages)
                    {
                        if (!RemovePackageWithWidget(widget)) break;
                    }
                }
            }
            else if(UserPackagesDragTarget.GetGlobalRect().HasPoint(mouseEvent.GlobalPosition))
            {
                GD.Print("Dragging packages into project");
                if (!dragOriginPackage.Package.IsInUse)
                {
                    foreach (PackageWidget widget in movedPackages)
                    {
                        if(!AddPackageWithWidget(widget)) break;
                    }
                }
            }

            EditorInterface.Singleton.GetResourceFilesystem().Scan();
        }

        public void OnRefreshClicked()
        {
            RefreshPackages();
        }

        public void OnCreateClicked()
        {
            PromptDialog.Title = "Name your new package";
            PromptDialog.Text = "";
            PromptDialog.Show();
            PromptDialog.ClosedOk += OnNewPackageNameSet;
        }

        private void OnNewPackageNameSet(object sender, EventArgs e)
        {
            CreateNewPackage(PromptDialog.Text);
            PromptDialog.ClosedOk -= OnNewPackageNameSet;
        }

        public void OnAddClicked()
        {
            foreach (PackageWidget widget in allWidgets)
            {
                if (widget.IsSelected)
                {
                    if(!AddPackageWithWidget(widget)) break;
                }
            }

            EditorInterface.Singleton.GetResourceFilesystem().Scan();
        }

        public void OnRemoveClicked()
        {
            foreach (PackageWidget widget in allWidgets)
            {
                if (widget.IsSelected)
                {
                    if(!RemovePackageWithWidget(widget)) break;
                }
            }

            EditorInterface.Singleton.GetResourceFilesystem().Scan();
        }

        public void OnConfigureClicked()
        {
            ConfigureGoPot configWindow = ConfigureWindow.Instantiate<ConfigureGoPot>();
            EditorInterface.Singleton.PopupDialogCentered(configWindow);
            configWindow.Show();
        }

        public override void _Ready()
        {
            // TODO: check if GoPot config file exists. if not require config to be set up first!
            // TODO: don't have both a dictionary AND list of PackageInfo
            packageManager = new PackageManager();
            allWidgets = new List<PackageWidget>();
            DragWidget.OnDragFinished += _OnDragFinished;
            DragWidget.allWidgets = allWidgets;

            ManifestDialog.OnSavedChanges += _OnSavedManifest;
            RefreshPackages();
        }

        private void _OnSavedManifest(object sender, EventArgs e)
        {
            foreach (PackageWidget widget in allWidgets)
            {
                widget.Refresh();
            }
        }

        private void _OnDragFinished(object sender, PackagesDragWidget.DragEventArgs e)
        {
            FinishDragSelectedPackages(e.DraggedPackage, e.ButtonEvent);
        }

        private void _OnEditPackageManifest(object sender, EventArgs e)
        {
            PackageWidget packageWidget = (PackageWidget)sender;
            ManifestDialog.ShowForPackage(packageManager, packageWidget.Package);
        }

        private void RefreshPackages()
        {
            packageManager.ScanPackages();

            // fill in UI with packages in appropriate lists
            bool haveAvailablePackages = false;
            bool haveUserPackages = false;
            foreach(PackageInfo package in packageManager.AllPackages)
            {
                PackageWidget packageWidget = PackageWidgetScene.Instantiate<PackageWidget>();
                packageWidget.Init(package, true);
                packageWidget.DragWidget = DragWidget;
                packageWidget.OnEditManifest += _OnEditPackageManifest;
                allWidgets.Add(packageWidget);
                if (package.IsInUse)
                {
                    UserPackagesContainer.AddChild(packageWidget);
                    haveUserPackages = true;
                }
                else
                {
                    AvailablePackagesContainer.AddChild(packageWidget);
                    haveAvailablePackages = true;
                }
            }

            AvailablePackagesEmptyLabel.Visible = !haveAvailablePackages;
            UserPackagesEmptyLabel.Visible = !haveUserPackages;
        }

        private void CreateNewPackage(string packageName)
        {
            packageName = packageName.Trim();
            if (string.IsNullOrEmpty(packageName))
            {
                AcceptDialog.DialogText = "Cannot save package with blank name!";
                AcceptDialog.Show();
                return;
            }

            if(packageManager.PackageExists(packageName))
            {
                AcceptDialog.DialogText = "A package named '" + packageName + "' already exists!";
                AcceptDialog.Show();
                return;
            }

            PackageInfo newPackage = packageManager.CreatePackage(packageName);
            PackageWidget newPackageWidget = PackageWidgetScene.Instantiate<PackageWidget>();
            newPackageWidget.Init(newPackage, true);
            newPackageWidget.OnEditManifest += _OnEditPackageManifest;
            newPackageWidget.DragWidget = DragWidget;
            AvailablePackagesEmptyLabel.Visible = false;
            AvailablePackagesContainer.AddChild(newPackageWidget);
            allWidgets.Add(newPackageWidget);
        }

        private bool AddPackageWithWidget(PackageWidget widget)
        {
            try
            {
                packageManager.AddToCurrentProject(widget.Package);
            }
            catch (Exception ex)
            {
                AcceptDialog.DialogText =
                    "Error while trying to add package link for " +
                    "\n" + ex.GetType().Name +
                    "\n\n" + ex.Message;
                AcceptDialog.Show();
                return false;
            }

            widget.IsSelected = false;
            widget.Package.IsInUse = true;
            widget.Reparent(UserPackagesContainer, false);
            UserPackagesEmptyLabel.Visible = false;
            AvailablePackagesEmptyLabel.Visible = AvailablePackagesContainer.GetChildCount() == 1;
            return true;
        }

        private bool RemovePackageWithWidget(PackageWidget widget)
        {
            PackageInfo package = widget.Package;
            try
            {
                packageManager.RemoveFromCurrentProject(widget.Package);
            }
            catch (Exception ex)
            {
                AcceptDialog.DialogText =
                    "Error while trying to remove link for " + package.Name +
                    "\n" + ex.GetType().Name +
                    "\n\n" + ex.Message;
                AcceptDialog.Show();
                return false;
            }

            widget.IsSelected = false;
            widget.Package.IsInUse = false;
            widget.Reparent(AvailablePackagesContainer, false);
            AvailablePackagesEmptyLabel.Visible = false;
            UserPackagesEmptyLabel.Visible = UserPackagesContainer.GetChildCount() == 1;
            return true;
        }
    }
}