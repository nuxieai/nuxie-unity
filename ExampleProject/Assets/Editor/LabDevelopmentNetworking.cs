using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
#if UNITY_ANDROID
using UnityEditor.Android;
#endif
namespace Nuxie.Unity.Example
{
#if UNITY_ANDROID
    internal sealed class LabAndroidNetworking : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 200;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var directory = Path.Combine(Directory.GetParent(path).FullName,"launcher/src/debug");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory,"AndroidManifest.xml"),"<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"><application android:usesCleartextTraffic=\"true\" /></manifest>");
        }
    }
#endif
#if UNITY_IOS
    internal sealed class LabIosNetworking : IPostprocessBuildWithReport
    {
        public int callbackOrder => 200;
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS || (report.summary.options & BuildOptions.Development) == 0) return;
            var path = Path.Combine(report.summary.outputPath,"Info.plist");
            var plist = new PlistDocument(); plist.ReadFromFile(path);
            plist.root.CreateDict("NSAppTransportSecurity").SetBoolean("NSAllowsLocalNetworking",true);
            plist.WriteToFile(path);
        }
    }
#endif
}
