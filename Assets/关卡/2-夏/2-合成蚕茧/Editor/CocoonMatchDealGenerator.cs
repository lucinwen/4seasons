using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ShiXu.CocoonMatch.Editor
{
    public static class CocoonMatchDealGenerator
    {
        public static string Generate(CocoonMatchLevel data)
        {
            data.tileSize = 88;
            data.placements.Clear();
            int[] widths = { 8, 6, 4 };
            for (int layer = 0; layer < 3; layer++)
                for (int row = 0; row < 3; row++)
                    for (int col = 0; col < widths[layer]; col++)
                    {
                        float x = (col - (widths[layer] - 1) * .5f) * 92;
                        // Spread the middle layer out symmetrically to straddle pairs below.
                        if (layer == 1) x += Mathf.Sign(x) * 46;
                        // Top cards sit over the four middle columns; all layers mirror about x=0.
                        if (layer == 2) x += Mathf.Sign(x) * 46;
                        data.placements.Add(new CocoonPlacement {
                            layer = layer, position = new Vector2(x, (row - 1) * 92 + (layer == 1 ? 12 : layer == 2 ? -12 : 0))
                        });
                    }

            int[] bestPatterns = null;
            List<int> bestSolution = null;
            double bestScore = double.MaxValue;
            int selectedSeed = data.seed;
            // Keep a known legal path, but interleave THREE open pairs along that path.
            // It must temporarily use six of seven slots instead of clearing each consecutive triple.
            for (int attempt = 0; attempt < 1000; attempt++)
            {
                var random = new System.Random(20261116 + attempt * 7919);
                var remaining = new HashSet<int>(Enumerable.Range(0, 54));
                var path = new List<int>();
                while (remaining.Count > 0)
                {
                    var free = remaining.Where(i => !remaining.Any(j =>
                        data.placements[j].layer > data.placements[i].layer && CocoonMatchModel.Overlaps(data, i, j))).OrderBy(i => i).ToList();
                    int next = free[random.Next(free.Count)];
                    path.Add(next);
                    remaining.Remove(next);
                }
                var groupOrder = new List<int>();
                for (int pass = 0; pass < 2; pass++)
                {
                    var kinds = Enumerable.Range(0, 9).ToList();
                    Shuffle(kinds, random);
                    groupOrder.AddRange(kinds);
                }
                for (int block = 0; block < 6; block++)
                {
                    var chunk = Enumerable.Range(0, 9).Select(i => groupOrder[block * 3 + i % 3]).ToList();
                    // Interleave all three pairs; the saved path needs six occupied slots before clearing.
                    for (int i = 0; i < 9; i++) data.placements[path[block * 9 + i]].pattern = chunk[i];
                }

                var start = new CocoonMatchModel(data);
                var initial = Enumerable.Range(0, 54).Where(start.IsExposed).GroupBy(i => data.placements[i].pattern).ToArray();
                int readyTriples = initial.Count(g => g.Count() >= 3);
                int diversity = initial.Length;
                if (readyTriples > 1 || diversity < 9) continue;
                // One third is a repeatable BOT calibration target, never a claim about real players.
                // Every candidate still retains its explicit legal solution.
                float rate = EstimateGreedyWinRate(data, 128);
                double score = Math.Abs(rate - 1f / 3f);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestPatterns = data.placements.Select(t => t.pattern).ToArray();
                    bestSolution = path;
                    selectedSeed = 20261116 + attempt * 7919;
                }
                if (score < .012) break;
            }
            if (bestPatterns == null) throw new InvalidOperationException("未找到符合难度约束的牌面。");
            for (int i = 0; i < 54; i++) data.placements[i].pattern = bestPatterns[i];
            data.seed = selectedSeed;
            data.solution = bestSolution;
            TightenOpening(data);
            data.ValidateData();
            var verifier = new CocoonMatchModel(data);
            int peak = 0;
            foreach (int id in data.solution)
            {
                if (!verifier.TryCollect(id, out var move)) throw new InvalidOperationException("生成的牌面无解。");
                peak = Math.Max(peak, move.trayBeforeMatch.Length);
            }
            if (verifier.State != CocoonGameState.Won) throw new InvalidOperationException("生成的序列没有通关。");
            var preview = new CocoonMatchModel(data);
            int exposed = Enumerable.Range(0, 54).Count(preview.IsExposed);
            int initialTriples = Enumerable.Range(0, 54).Where(preview.IsExposed).GroupBy(i => data.placements[i].pattern).Count(g => g.Count() >= 3);
            return $"54 cards / 3 layers / 88px / mirror symmetry; {exposed} exposed, {initialTriples} immediately available triples; known solution peak including matching tile: {peak}/7; greedy baseline: {EstimateGreedyWinRate(data, 128):P1}";
        }

        // A deliberately simple comparison policy: take a triple, then a pair, then another card.
        // This is a repeatable relative difficulty signal, NOT an estimate of human win rates.
        public static float EstimateGreedyWinRate(CocoonMatchLevel data, int runs)
        {
            int won = 0;
            for (int run = 0; run < runs; run++)
            {
                var random = new System.Random(run * 3571 + 43);
                var game = new CocoonMatchModel(data);
                while (game.State == CocoonGameState.Playing)
                {
                    var free = Enumerable.Range(0, data.placements.Count).Where(game.CanCollect).ToList();
                    if (free.Count == 0) break;
                    int best = -1;
                    var choices = new List<int>();
                    foreach (int id in free)
                    {
                        int same = game.Tray.Count(t => data.placements[t].pattern == data.placements[id].pattern);
                        if (same > best) { best = same; choices.Clear(); }
                        if (same == best) choices.Add(id);
                    }
                    game.TryCollect(choices[random.Next(choices.Count)], out _);
                }
                if (game.State == CocoonGameState.Won) won++;
            }
            return (float)won / runs;
        }

        private static void TightenOpening(CocoonMatchLevel data)
        {
            var initial = new CocoonMatchModel(data);
            var exposed = Enumerable.Range(0, 54).Where(initial.IsExposed).ToArray();
            var counts = exposed.GroupBy(id => data.placements[id].pattern).ToDictionary(g => g.Key, g => g.Count());
            var donors = exposed.Where(id => counts[data.placements[id].pattern] >= 3).ToArray();
            var receivers = Enumerable.Range(0, 54).Where(id => !initial.IsExposed(id) &&
                counts.TryGetValue(data.placements[id].pattern, out int count) && count < 2).ToArray();
            float ceiling = EstimateGreedyWinRate(data, 128);
            const float target = 1f / 3f;
            float best = Math.Abs(ceiling - target);
            int[] selected = null;
            foreach (int a in donors)
                foreach (int b in receivers)
                {
                    int pa = data.placements[a].pattern, pb = data.placements[b].pattern;
                    data.placements[a].pattern = pb;
                    data.placements[b].pattern = pa;
                    // Swaps preserve six of each pattern, but must also preserve the saved legal path.
                    var game = new CocoonMatchModel(data);
                    bool legal = true;
                    foreach (int id in data.solution)
                        if (!game.TryCollect(id, out _)) { legal = false; break; }
                    if (legal && game.State == CocoonGameState.Won)
                    {
                        float rate = EstimateGreedyWinRate(data, 128);
                        if (Math.Abs(rate - target) < best)
                        {
                            best = Math.Abs(rate - target);
                            selected = data.placements.Select(p => p.pattern).ToArray();
                        }
                    }
                    data.placements[a].pattern = pa;
                    data.placements[b].pattern = pb;
                }
            if (selected != null)
                for (int i = 0; i < 54; i++) data.placements[i].pattern = selected[i];
        }

        private static void Shuffle(List<int> values, System.Random random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int temp = values[i]; values[i] = values[j]; values[j] = temp;
            }
        }
    }
}
