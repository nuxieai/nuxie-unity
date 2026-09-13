using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
#if UNITY_ANDROID
using UnityEditor.Android;
#endif
namespace Nuxie.Unity.Editor
{
    internal static class PackageFiles
    {
        internal static string Root => UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(NuxieSettings).Assembly)?.resolvedPath
            ?? throw new BuildFailedException("Install com.nuxie.unity through Package Manager before building");
        internal static JObject Pins => JObject.Parse(File.ReadAllText(Path.Combine(Root,"NATIVE-PINS.json")));
        internal static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (var file in Directory.GetFiles(source)) File.Copy(file,Path.Combine(target,Path.GetFileName(file)),true);
            foreach (var dir in Directory.GetDirectories(source)) CopyDirectory(dir,Path.Combine(target,Path.GetFileName(dir)));
        }
    }
    internal sealed class NuxieBuildPreflight : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS && report.summary.platform != BuildTarget.Android)
                throw new BuildFailedException("Nuxie supports iOS and Android players; use the explicit simulator in Editor");
            if (report.summary.platform == BuildTarget.Android && !Directory.Exists(Path.Combine(PackageFiles.Root,"Artifacts~/maven/ai/nuxie/nuxie-unity-bridge")))
                throw new BuildFailedException("Nuxie native artifacts are missing. Use the prepared UPM release, or run python3 scripts/prepare-native.py from the SDK source");
        }
    }
#if UNITY_IOS
    internal sealed class NuxieIosExport : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
            string path = PBXProject.GetPBXProjectPath(report.summary.outputPath);
            var project = new PBXProject(); project.ReadFromFile(path);
            var pin = PackageFiles.Pins["ios"];
            var target = project.GetUnityFrameworkTargetGuid();
            // PBXProject owns serialization and reuses matching package references on append exports.
            var reference = project.AddRemotePackageReferenceAtRevision((string)pin["repository"],(string)pin["revision"]);
            project.AddRemotePackageFrameworkToProject(target,"Nuxie",reference,false);
            var resource = project.FindFileGuidByProjectPath("Nuxie_Nuxie.bundle");
            if (string.IsNullOrEmpty(resource)) resource = project.AddFile("Nuxie_Nuxie.bundle","Nuxie_Nuxie.bundle",PBXSourceTree.Build);
            project.AddFileToBuildSection(target,project.AddResourcesBuildPhase(target),resource);
            project.SetBuildProperty(target,"SWIFT_VERSION","5.0");
            project.AddBuildPropertyForConfig(project.BuildConfigByName(target,"Debug"),"SWIFT_ACTIVE_COMPILATION_CONDITIONS","DEBUG");
            project.SetBuildProperty(target,"CLANG_ENABLE_MODULES","YES");
            project.SetBuildProperty(project.GetUnityMainTargetGuid(),"ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES","YES");
            project.WriteToFile(path);
        }
    }
#endif
#if UNITY_ANDROID
    internal sealed class NuxieAndroidExport : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 100;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var root = Directory.GetParent(path).FullName;
            PackageFiles.CopyDirectory(Path.Combine(PackageFiles.Root,"Artifacts~/maven"),Path.Combine(root,"nuxie-maven"));
            var settings = Path.Combine(root,"settings.gradle");
            AppendOnce(settings,"// Nuxie repository", "\ndependencyResolutionManagement { repositories { maven { url uri(\"${rootDir}/nuxie-maven\") } } }\n");
            AppendOnce(Path.Combine(path,"build.gradle"),"// Nuxie dependency", "\ndependencies { implementation 'ai.nuxie:nuxie-unity-bridge:0.2.0' }\n");
            // The pinned native SDK supplies the newer shared C++ runtime. Keep one copy
            // in the final app instead of selecting an arbitrary duplicate in the launcher.
            AppendOnce(Path.Combine(path,"build.gradle"),"// Nuxie shared C++ runtime", "\nandroid { packaging { jniLibs { excludes += ['**/libc++_shared.so'] } } }\n");
        }
        private static void AppendOnce(string file,string marker,string contents)
        { var text = File.ReadAllText(file); if (!text.Contains(marker)) File.AppendAllText(file,"\n" + marker + contents); }
    }
#endif
    [InitializeOnLoad]
    internal static class NuxiePlayModeLifecycle
    {
        static NuxiePlayModeLifecycle()
        {
            EditorApplication.playModeStateChanged -= Changed;
            EditorApplication.playModeStateChanged += Changed;
        }
        private static void Changed(PlayModeStateChange change)
        { if (change == PlayModeStateChange.ExitingPlayMode) Nuxie.ResetRuntime(); }
    }
}
