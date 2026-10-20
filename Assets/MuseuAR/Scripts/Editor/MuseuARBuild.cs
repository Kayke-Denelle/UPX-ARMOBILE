using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MuseuAR.EditorTools
{
    /// <summary>
    /// Menu "Museu AR > 4. Gerar APK (Android)": gera Builds/MuseuAR.apk com a cena do museu.
    /// O APK pode ser copiado para o celular (ou enviado por link) e instalado direto.
    /// Um resumo do último build fica em Builds/build-result.txt.
    /// </summary>
    public static class MuseuARBuild
    {
        public const string ApkPath = "Builds/MuseuAR.apk";
        public const string ResultPath = "Builds/build-result.txt";

        [MenuItem("Museu AR/4. Gerar APK (Android)", priority = -7)]
        static void BuildMenu() => ScheduleBuild(interactive: true);

        /// <summary>O build não pode rodar dentro de um callback do editor: agenda para o próximo ciclo.</summary>
        public static void ScheduleBuild(bool interactive)
        {
            EditorApplication.delayCall += () => BuildAndroidApk(interactive);
        }

        public static bool BuildAndroidApk(bool interactive)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));
            if (File.Exists(ResultPath)) File.Delete(ResultPath);

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
                !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Finish(interactive, false, "Não foi possível mudar a plataforma para Android. Instale o módulo Android pelo Unity Hub.");
                return false;
            }

            EditorUserBuildSettings.buildAppBundle = false;   // .apk instala direto; .aab é só para a Play Store
            var options = new BuildPlayerOptions
            {
                scenes = new[] { MuseuARSceneBuilder.MuseumScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            bool ok = summary.result == BuildResult.Succeeded;

            var text = new StringBuilder();
            text.AppendLine($"result={summary.result} tamanho={summary.totalSize / 1048576.0:F1} MB tempo={summary.totalTime:mm\\:ss} erros={summary.totalErrors} avisos={summary.totalWarnings}");
            foreach (var step in report.steps)
                foreach (var message in step.messages)
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                        text.AppendLine($"[{step.name}] {message.content}");

            Finish(interactive, ok, text.ToString());
            return ok;
        }

        static void Finish(bool interactive, bool ok, string text)
        {
            File.WriteAllText(ResultPath, text);
            if (ok) Debug.Log("[Museu AR] APK gerado em " + ApkPath + "\n" + text);
            else Debug.LogError("[Museu AR] Falha no build do APK\n" + text);

            if (!interactive) return;
            if (ok)
            {
                EditorUtility.DisplayDialog("Museu AR", "APK gerado em " + ApkPath +
                    "\n\nCopie para o celular Android e instale (permita \"instalar apps desconhecidos\").", "OK");
                EditorUtility.RevealInFinder(ApkPath);
            }
            else EditorUtility.DisplayDialog("Museu AR", "O build falhou. Veja o Console.\n\n" + text, "OK");
        }
    }
}
