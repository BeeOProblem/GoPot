using Godot;
using System;

namespace GoPot
{
    [Tool]
    public partial class GoPot : EditorPlugin
    {
        private GoPotEditorWindow window;

        public static string GetConfigPath()
        {
            return ProjectSettings.GlobalizePath("user://gopot.ini");
        }

        public override void _EnterTree()
        {
            PackedScene windowScene = GD.Load<PackedScene>("res://addons/GoPot/GoPotEditorWindow.tscn");
            window = windowScene.Instantiate<GoPotEditorWindow>();
            AddControlToBottomPanel(window, "GoPot");
        }

        public override void _ExitTree()
        {
            RemoveControlFromBottomPanel(window);
            window.Free();
        }
    }
}