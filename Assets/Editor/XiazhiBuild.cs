using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class XiazhiBuild
{
    public static void VerifyRules()
    {
        if (XiazhiPuzzle.RotateMask(1) != 2 || XiazhiPuzzle.RotateMask(8) != 1) throw new Exception("Clockwise mask rotation");
        int[] minima = {5,16,19};
        for (int level = 0; level < XiazhiPuzzle.LevelCount; level++)
        {
            var game = new XiazhiPuzzle(level);
            Check(!game.Solved && game.Moves == 0, "unsolved start");
            Check(!game.Rotate(game.Start) && !game.Rotate(game.End), "fixed endpoints");
            int[] answer = game.Solve();
            Check(answer != null && answer.Sum() == minima[level], "prototype parity minimum");
            for (int seed = 0; seed < 100; seed++)
            {
                game.Load(level);
                for (int cell = 0; cell < 16; cell++)
                    for (int turn = 0; turn < ((seed*37+cell*13)^(seed+cell*7))%4; turn++) game.Rotate(cell);
                answer = game.Solve(); Check(answer != null, "recoverable shuffled state");
                for (int cell = 0; cell < 16; cell++) for (int turn = 0; turn < answer[cell]; turn++) game.Rotate(cell);
                Check(game.Solved, "valid solved path");
            }
            game.Load(level);
            int hintCount = 0;
            while (!game.Solved && hintCount++ < 40)
            {
                Check(game.Hint(out int cell, out int turns), "usable hint");
                for (int t = 0; t < turns; t++) game.Rotate(cell);
            }
            Check(game.Solved, "hints can solve the whole board");
            Check(!game.Rotate(5), "solved rotation lock");
            Check(game.Undo() && !game.Solved, "undo after success");
            game.Load(level); Check(game.Moves == 0 && game.Hints == 0, "restart reset");
        }
        var summer = WorkshopProgress.Items(WorkshopSeason.Summer);
        Check(summer.SequenceEqual(new[] {WorkshopItem.Salt, WorkshopItem.Suoxu, WorkshopItem.Spool}), "original three-item recipe");
        Check(WorkshopProgress.ProductName(WorkshopSeason.Summer) == "丝束", "original product");
        Debug.Log("[XiazhiVerify] PASS: 3 boards, prototype parity, 300 recovery states, hints, undo, reset and original summer recipe.");
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception("Xiazhi verification failed: " + message); }
    public static void BuildReview() { Build(true); }
    public static void BuildRelease() { Build(false); }
    public static void BuildSpringReview() { SpringCollectionChecks.Verify(); Build(true, true); }
    public static void BuildSeasonReview()
    {
        GameWorkflowMaintenance.FinalizeSpringCleanup();
        SeasonWorkflowChecks.Verify();
        GameWorkflowMaintenance.ValidateScenes();
        Build(true, false, true);
    }
    private static void Build(bool review, bool spring = false, bool seasons = false)
    {
        VerifyRules();
        string integration = Directory.GetParent(Application.dataPath).Parent.FullName;
        string output = Path.Combine(integration,"Outputs",seasons ? "SeasonReview" : spring ? "SpringReview" : review ? "XiazhiReview" : "FourSeasons_Xiazhi");
        Directory.CreateDirectory(output);
        string oldCompany = PlayerSettings.companyName, oldProduct = PlayerSettings.productName;
        try
        {
            if (review) { PlayerSettings.companyName = "Jiangnan Studio QA"; PlayerSettings.productName = seasons ? "四时织坊_春夏测试" : spring ? "四时织坊_春季测试" : "四时织坊_夏至测试"; }
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = Path.Combine(output, seasons ? "四时织坊_春夏测试.exe" : spring ? "四时织坊_春季测试.exe" : review ? "四时织坊_夏至测试.exe" : "四时织坊.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = review ? BuildOptions.Development : BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            File.WriteAllText(Path.Combine(output,"build-result.json"),JsonUtility.ToJson(new Result
            {
                success = report.summary.result == BuildResult.Succeeded,
                errors = report.summary.totalErrors, warnings = report.summary.totalWarnings,
                bytes = report.summary.totalSize, product = PlayerSettings.productName,
                scenes = options.scenes.Length
            },true));
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Player build failed: " + report.summary.result);
            Debug.Log("[XiazhiBuild] SUCCESS " + options.locationPathName);
        }
        finally
        {
            PlayerSettings.companyName = oldCompany; PlayerSettings.productName = oldProduct;
            AssetDatabase.SaveAssets();
        }
    }
    [Serializable] private sealed class Result
    {
        public bool success;
        public int errors, warnings;
        public ulong bytes;
        public string product;
        public int scenes;
    }
}
