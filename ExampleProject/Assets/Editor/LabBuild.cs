using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
namespace Nuxie.Unity.Example
{
    public static class LabBuild
    {
        public static void Android()
        {
            PlayerSettings.runInBackground = true;
            PlayerSettings.Android.startInFullscreen = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"ai.nuxie.unity.lab");
            Build(BuildTarget.Android,"Build/Android");
        }
        public static void IosSimulator()
        {
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
            PlayerSettings.iOS.simulatorSdkArchitecture = AppleMobileArchitectureSimulator.ARM64;
            ConfigureIos();
            Build(BuildTarget.iOS,"Build/iOS-Simulator");
        }
        public static void Ios()
        {
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            ConfigureIos();
            Build(BuildTarget.iOS,"Build/iOS");
        }
        private static void ConfigureIos()
        {
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS,ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS,"ai.nuxie.unity.lab");
        }
        private static void Build(BuildTarget target,string output)
        {
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] {"Assets/Scenes/SdkLab.unity","Assets/Scenes/Gameplay.unity"},target = target,locationPathName = output,options = BuildOptions.Development });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("SDK Lab build failed: " + report.summary.result);
        }
    }
}
