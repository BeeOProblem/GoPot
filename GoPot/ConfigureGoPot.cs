using Godot;
using System;
using System.IO;

namespace GoPot
{
    [Tool]
    public partial class ConfigureGoPot : Window
    {
        [Export]
        LineEdit PackagePath;

        public void OnOkClicked()
        {
            SaveWithPathAndClose(PackagePath.Text);
        }

        public void OnTextSubmitted(string newPath)
        {
            SaveWithPathAndClose(newPath);
        }

        public void OnCancelClicked()
        {
            Hide();
        }

        public void OnBrowseClicked()
        {

        }

        public override void _Ready()
        {
            ConfigFile config = new ConfigFile();
            Error res = config.Load("user://gopot.cfg");
            if (res != Error.Ok)
            {
                GD.PrintErr("cannot read gopot config file ", res, " filesystem path ", ProjectSettings.GlobalizePath("user://gopot.cfg"));
            }

            string packageRoot = config.GetValue("main", "package_root", "").AsString();
            if (string.IsNullOrEmpty(packageRoot))
            {
                packageRoot = Path.GetFullPath(ProjectSettings.GlobalizePath("res://.") + "/../GoPotPackages");
                config.SetValue("main", "package_root", packageRoot);
            }

            PackagePath.Text = packageRoot;
            res = config.Save("user://gopot.cfg");
            if (res != Error.Ok)
            {
                GD.PrintErr("cannot save gopot config file ", res, " filesystem path ", ProjectSettings.GlobalizePath("user://gopot.cfg"));
            }
        }

        private void SaveWithPathAndClose(string newPath)
        {
            ConfigFile config = new ConfigFile();
            config.SetValue("main", "package_root", newPath);
            Error res = config.Save("user://gopot.cfg");
            if (res != Error.Ok)
            {
                // TODO: popup?
                GD.PrintErr("cannot save gopot config file ", res, " filesystem path ", ProjectSettings.GlobalizePath("user://gopot.cfg"));
            }

            Hide();
        }
    }
}