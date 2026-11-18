using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

// Menu: Pony Game > Build WebGL (docs). Output goes to <project>/docs for GitHub Pages.
public static class WebGLBuilder
{
    [MenuItem("Pony Game/Build WebGL (docs)")]
    public static void Build()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.template = "APPLICATION:Minimal";
        PlayerSettings.productName = "Sparkle Pony - Heart Catch";
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);

        var opts = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Menu.unity", "Assets/Scenes/Game.unity" },
            locationPathName = "docs",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("WebGL build: " + report.summary.result + ", size " + (report.summary.totalSize / 1048576) + " MB");
    }
}
