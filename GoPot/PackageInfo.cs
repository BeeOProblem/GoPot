using System;
using System.IO;
using System.Collections.Generic;
using Godot;

namespace GoPot
{
    public class PackageInfo
    {
        public string DirectoryPath { get; private set; }
        public string DirectoryName { get; private set; }
        public string Name { get; set; }
        public List<PackageInfo> Dependencies { get; set; }
        public bool IsInUse { get; set; }

        public PackageInfo(string packagePath)
        {
            DirectoryPath = packagePath;
            DirectoryName = Path.GetFileName(packagePath);
            Dependencies = new List<PackageInfo>();
            Name = DirectoryName;
        }

        public PackageInfo(string packagePath, string name)
        {
            DirectoryPath = packagePath;
            DirectoryName = Path.GetFileName(packagePath);
            Dependencies = new List<PackageInfo>();
            Name = name;
        }

        public void LoadManifest(Dictionary<string, PackageInfo> packages)
        {
            ConfigFile manifestFile = new ConfigFile();
            string manifestPath = Path.Combine(DirectoryPath, "manifest.cfg");
            GD.Print("Loading manifest ", manifestPath);
            manifestFile.Load(manifestPath);
            Name = (string)manifestFile.GetValue("manifest", "description", DirectoryName);

            if (manifestFile.HasSection("dependencies"))
            {
                foreach (string packageName in manifestFile.GetSectionKeys("dependencies"))
                {
                    if (packages.ContainsKey(packageName))
                    {
                        PackageInfo dependency = packages[packageName];
                        Dependencies.Add(dependency);
                    }
                    else
                    {
                        GD.PushWarning("Package ", DirectoryName, " depends on package ",  packageName, " which does not exist");
                    }
                }
            }
        }

        public void SaveManifest()
        {
            ConfigFile manifestFile = new ConfigFile();
            string manifestPath = Path.Combine(DirectoryPath, "manifest.cfg");
            GD.Print("Saving manifest ", manifestPath);

            manifestFile.SetValue("manifest", "description", Name);
            foreach (PackageInfo package in Dependencies)
            {
                manifestFile.SetValue("dependencies", package.DirectoryName, "");
            }

            manifestFile.Save(manifestPath);
        }
    }
}