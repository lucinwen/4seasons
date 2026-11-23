#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JiangnanDye.MazeGame.EditorTools
{
    /// <summary>
    /// 为迷宫游戏构建玩家动画（a-idle 做待机, a-walk 做走路）和 AnimatorController。
    /// 菜单：Tools/江南迷宫/构建玩家动画
    /// </summary>
    public static class MazeAnimatorBuilder
    {
        private const string AnimDir = "Assets/3-迷宫（秋分）game/Game/Art/Animations";
        private const string IdleSpriteDir = "Assets/a-idle";
        private const string WalkSpriteDir = "Assets/a-walk";

        [MenuItem("Tools/江南迷宫/构建玩家动画")]
        public static void Build()
        {
            // 1. 对齐 PPU 和 pivot
            EnsureAligned(IdleSpriteDir);
            EnsureAligned(WalkSpriteDir);

            if (!AssetDatabase.IsValidFolder(AnimDir))
            {
                Directory.CreateDirectory(AnimDir);
                AssetDatabase.Refresh();
            }

            // 2. 读帧
            Sprite[] idleFrames = LoadSortedSprites(IdleSpriteDir);
            Sprite[] walkFrames = LoadSortedSprites(WalkSpriteDir);
            Debug.Log($"[MazeAnim] Idle={idleFrames.Length}帧, Walk={walkFrames.Length}帧");

            // binding：Animator 挂在 Player 根节点，Visual 是子物体，path="Visual"（与 4-寻找染料 完全一致）
            var binding = new EditorCurveBinding
            {
                path = "Visual",
                propertyName = "m_Sprite",
                type = typeof(SpriteRenderer)
            };

            // Idle clip：8 fps 循环呼吸
            var idleClip = new AnimationClip
            {
                name = "MazePlayerIdle",
                legacy = false,
                frameRate = 8f,
                wrapMode = WrapMode.Loop
            };
            var idleKeys = new ObjectReferenceKeyframe[idleFrames.Length];
            for (int i = 0; i < idleFrames.Length; i++)
                idleKeys[i] = new ObjectReferenceKeyframe { time = i / 8f, value = idleFrames[i] };
            AnimationUtility.SetObjectReferenceCurve(idleClip, binding, idleKeys);
            Save(idleClip, AnimDir + "/MazePlayerIdle.anim");

            // Walk clip：10 fps 循环
            var walkClip = new AnimationClip
            {
                name = "MazePlayerWalk",
                legacy = false,
                frameRate = 10f,
                wrapMode = WrapMode.Loop
            };
            var walkKeys = new ObjectReferenceKeyframe[walkFrames.Length];
            for (int i = 0; i < walkFrames.Length; i++)
                walkKeys[i] = new ObjectReferenceKeyframe { time = i / 10f, value = walkFrames[i] };
            AnimationUtility.SetObjectReferenceCurve(walkClip, binding, walkKeys);
            Save(walkClip, AnimDir + "/MazePlayerWalk.anim");

            // 4. Controller
            string ctrlPath = AnimDir + "/MazePlayer.controller";
            if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            var sm = ctrl.layers[0].stateMachine;

            var idleState = sm.AddState("Idle");
            idleState.motion = idleClip;
            sm.defaultState = idleState;

            var walkState = sm.AddState("Walk");
            walkState.motion = walkClip;

            ctrl.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

            // Idle -> Walk
            var t1 = idleState.AddTransition(walkState);
            t1.AddCondition(AnimatorConditionMode.If, 1f, "IsMoving");
            t1.hasExitTime = false; t1.duration = 0.05f;

            // Walk -> Idle
            var t2 = walkState.AddTransition(idleState);
            t2.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
            t2.hasExitTime = false; t2.duration = 0.05f;

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MazeAnim] AnimatorController 构建完成: " + ctrlPath);
        }

        private static void Save(UnityEngine.Object obj, string path)
        {
            var old = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (old != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(obj, path);
        }

        private static Sprite[] LoadSortedSprites(string folder)
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            Sprite[] frames = new Sprite[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[i]));
            Array.Sort(frames, (a, b) => NaturalCompare(a.name, b.name));
            return frames;
        }

        private static void EnsureAligned(string folder)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            int n = 0;
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;

                ti.spritePixelsPerUnit = 64;
                var s = new TextureImporterSettings();
                ti.ReadTextureSettings(s);
                s.spriteAlignment = (int)SpriteAlignment.Custom;
                s.spritePivot = new Vector2(0.5f, 0.02f);
                ti.SetTextureSettings(s);
                ti.SaveAndReimport();
                n++;
            }
            Debug.Log($"[MazeAnim] {folder}: {n} 帧已对齐 PPU=64 pivot=(0.5,0.02)");
        }

        private static int NaturalCompare(string a, string b)
        {
            var rgx = new Regex(@"\d+");
            var ma = rgx.Match(a); var mb = rgx.Match(b);
            if (ma.Success && mb.Success)
            {
                int c = int.Parse(ma.Value).CompareTo(int.Parse(mb.Value));
                if (c != 0) return c;
            }
            return string.Compare(a, b, StringComparison.Ordinal);
        }
    }
}
#endif
