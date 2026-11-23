using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ShiXu.CocoonMatch.Editor
{
    // Editor-only rule regression checks and read-only observations for MCP playtesting.
    public static class CocoonMatchVerification
    {
        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        [MenuItem("Tools/时序小织匠/验证收茧玩法边界")]
        public static string RunRules()
        {
            var level = AssetDatabase.LoadAssetAtPath<CocoonMatchLevel>(CocoonMatchSceneBuilder.Root + "/Data/CocoonMatchLevel.asset");
            level.ValidateData();
            Check(level.placements.Count == 54 && level.placements.GroupBy(t => t.layer).Count() == 3,
                "必须保留54张、3层。");
            Check(level.placements.GroupBy(t => t.pattern).Count() == 9 &&
                level.placements.GroupBy(t => t.pattern).All(g => g.Count() == 6), "必须每种6张。");
            var game = new CocoonMatchModel(level);
            int blocked = Enumerable.Range(0, 54).First(i => !game.IsExposed(i));
            Check(!game.TryCollect(blocked, out _) && game.BoardCount == 54, "遮挡牌不能入槽。");
            Check(!game.TryCollect(-1, out _) && !game.TryCollect(54, out _), "越界编号必须拒绝。");
            int peak = 0, triples = 0;
            foreach (int id in level.solution)
            {
                Check(game.IsExposed(id) && game.TryCollect(id, out _), "通关路径中出现不可取牌。");
                Check(!game.TryCollect(id, out _), "已收牌不能重复入槽。");
                Check(game.BoardCount + game.Tray.Count + game.ClearedCount == 54, "牌数守恒失败。");
                Check(game.Tray.Count <= 6, "成功路径不应满槽失败。");
                peak = Math.Max(peak, game.Tray.Count);
                triples = game.ClearedCount / 3;
            }
            Check(game.State == CocoonGameState.Won && game.BoardCount == 0 && game.Tray.Count == 0 && triples == 18,
                "胜利必须清空场上和槽内全部54张。");
            Check(!game.TryCollect(0, out _), "结束后不能继续操作。");

            var fixture = ScriptableObject.CreateInstance<CocoonMatchLevel>();
            try
            {
                fixture.tileSize = 88;
                fixture.patterns = level.patterns.Take(7).ToArray();
                for (int kind = 0; kind < 7; kind++)
                    for (int copy = 0; copy < 3; copy++)
                        fixture.placements.Add(new CocoonPlacement { pattern = kind, position = new Vector2((kind * 3 + copy) * 100, 0) });
                fixture.ValidateData();
                var seventh = new CocoonMatchModel(fixture);
                foreach (int id in new[] { 0, 1, 3, 4, 6, 7 }) Check(seventh.TryCollect(id, out _), "六格准备失败。");
                Check(seventh.Tray.Count == 6 && seventh.TryCollect(2, out _), "第七张应可选择。");
                Check(seventh.State == CocoonGameState.Playing && seventh.Tray.Count == 4 && seventh.ClearedCount == 3,
                    "第七张组成三消时必须先消除，不能判负。");
                var lost = new CocoonMatchModel(fixture);
                foreach (int id in new[] { 0, 3, 6, 9, 12, 15, 18 }) Check(lost.TryCollect(id, out _), "满槽准备失败。");
                Check(lost.State == CocoonGameState.Lost && lost.Tray.Count == 7 && !lost.TryCollect(1, out _),
                    "七张无三消应判负并锁定。");
                var sorted = new CocoonMatchModel(fixture);
                foreach (int id in new[] { 0, 3, 1 }) sorted.TryCollect(id, out _);
                Check(sorted.Tray.SequenceEqual(new[] { 0, 1, 3 }), "同类应相邻排序。");
                Check(sorted.TryCollect(2, out var match) && match.matched.Length == 3 && sorted.Tray.SequenceEqual(new[] { 3 }),
                    "三消后应补位且保留其他牌。");
            }
            finally { UnityEngine.Object.DestroyImmediate(fixture); }
            return "PASS: 54 cards / 9 x 6 / 3 layers; blocked and duplicate clicks; legal 54-move win; conservation; sorting and compaction; seventh-card triple; seven-card loss; terminal lock. " +
                $"Solution peak after matching {peak}/7; greedy calibration (128 runs): {CocoonMatchDealGenerator.EstimateGreedyWinRate(level, 128):P1}. " +
                CocoonMatchPresentation.Inspect();
        }

        [Serializable]
        public sealed class Observation
        {
            public bool ready;
            public int board, cleared, trayCount;
            public string state, nextTarget, blockedTarget, harvest;
            public bool startVisible, resultVisible, closedVisible, surfaceVisible;
            public int[] tray;
        }

        public static string Status()
        {
            var c = UnityEngine.Object.FindObjectOfType<CocoonMatchController>();
            if (c == null || c.Model == null) return JsonUtility.ToJson(new Observation { ready = false });
            var m = c.Model;
            int index = c.level.placements.Count - m.BoardCount;
            int blocked = Enumerable.Range(0, c.tiles.Count).FirstOrDefault(i => m.IsOnBoard(i) && !m.IsExposed(i));
            return JsonUtility.ToJson(new Observation {
                ready = !c.IsAnimating, board = m.BoardCount, cleared = m.ClearedCount,
                trayCount = m.Tray.Count, tray = m.Tray.ToArray(), state = m.State.ToString(),
                nextTarget = index < c.level.solution.Count ? c.tiles[c.level.solution[index]].gameObject.name : "",
                blockedTarget = m.IsOnBoard(blocked) && !m.IsExposed(blocked) ? c.tiles[blocked].gameObject.name : "",
                harvest = c.harvestText.text, startVisible = c.startPanel.activeSelf,
                resultVisible = c.resultPanel.activeSelf, closedVisible = c.closedPanel.activeSelf,
                surfaceVisible = c.gameSurface.activeSelf
            });
        }
    }
}
