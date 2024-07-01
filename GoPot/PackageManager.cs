using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GoPot
{
    internal class PackageManager
    {
        private JunctionPoint junction;
        private List<PackageInfo> allPackages;
        private Dictionary<string, PackageInfo> allPackagesByName;

        public IEnumerable<PackageInfo> AllPackages
        {
            get
            {
                return allPackages.AsEnumerable();
            }
        }

        public PackageManager() 
        {
            junction = new JunctionPoint();
            allPackages = new List<PackageInfo>();
            allPackagesByName = new Dictionary<string, PackageInfo>();
        }

        public string GetPackageRoot()
        {
            // TODO: this act of reading (and grabbing a default?) is duplicated in config window
            ConfigFile config = new ConfigFile();
            Error res = config.Load("user://gopot.cfg");
            if (res != Error.Ok)
            {
                GD.PrintErr("cannot read gopot config file ", res, " filesystem path ", ProjectSettings.GlobalizePath("user://gopot.cfg"));
            }

            return config.GetValue("main", "package_root", "").AsString();
        }

        public void ScanPackages()
        {
            string packageRoot = GetPackageRoot();

            // scan for available packages
            foreach (string dir in Directory.GetDirectories(packageRoot))
            {
                PackageInfo foundPackage = new PackageInfo(dir);
                allPackages.Add(foundPackage);
                allPackagesByName[foundPackage.DirectoryName] = foundPackage;
            }

            // load manifests
            foreach (PackageInfo package in allPackages)
            {
                package.LoadManifest(allPackagesByName);
            }

            // scan for included packages, warn if not symlinks
            JunctionPoint junction = new JunctionPoint();
            foreach (string dir in Directory.GetDirectories(ProjectSettings.GlobalizePath("res://.")))
            {
                // TODO: packages as dictionary
                if(junction.Exists(dir))
                {
                    GD.Print(dir, " is a package");
                    // TODO: if symlink and package name add to included packages list
                    foreach(PackageInfo package in allPackages)
                    {
                        if(package.DirectoryName.ToLower() == Path.GetFileName(dir.ToLower()))
                        {
                            package.IsInUse = true;
                            GD.Print(dir, " is in project package");
                            break;
                        }
                    }
                }
                else
                {
                    // TODO: if not symlink but has package name log error
                }
            }
        }

        public bool PackageExists(string packageName)
        {
            foreach (PackageInfo package in allPackages)
            {
                if (package.Name.ToLower() == packageName.ToLower())
                {
                    return true;
                }
            }

            return false;
        }

        public PackageInfo CreatePackage(string packageName)
        {
            string packageRoot = GetPackageRoot();
            string packagePath = Path.Combine(packageRoot, packageName);
            string manifestPath = Path.Combine(packagePath, "manifest.cfg");
            PackageInfo newPackage = new PackageInfo(packagePath, packageName);
            
            // TODO: Consider filling in manifest with actual data
            Directory.CreateDirectory(packagePath);
            File.Create(manifestPath).Close();
            allPackages.Add(newPackage);

            return newPackage;
        }

        public void AddToCurrentProject(PackageInfo package)
        {
            // TODO: do a case insensitive check so project is compatible w multiple filesystems
            //       even if the user's filesystem is just fine treating /Path /path and /pATH as different things
            string packageJunctionPath = GetPackageJunctionPath(package);
            if (Directory.Exists(packageJunctionPath) || File.Exists(packageJunctionPath))
            {
                throw new IOException("Cannot use package because a file or directory named " + package.DirectoryName + " already exists in your project!");
            }

            junction.Create(packageJunctionPath, package.DirectoryPath, false);
        }

        public void RemoveFromCurrentProject(PackageInfo package)
        {
            JunctionPoint junction = new JunctionPoint();
            string packageJunctionPath = GetPackageJunctionPath(package);
            junction.Delete(packageJunctionPath);
        }

        private string GetPackageJunctionPath(PackageInfo package)
        {
            string projectRoot = ProjectSettings.GlobalizePath("res://.");
            return Path.Combine(projectRoot, package.DirectoryName);
        }
    }
}
