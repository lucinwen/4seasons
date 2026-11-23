#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace JiangnanDye.EditorTools
{
    /// <summary>构建玩家 AnimatorController：Idle + Walk 两状态，IsMoving 条件过渡。</summary>
    public static class PlayerAnimatorBuilder
    {
        private const string AnimDir = "Assets/4-寻找染料game/Game/Art/Animations";
        private const string WalkSpriteDir = "Assets/a-walk";
        private const string RunSpriteDir = "Assets/a-run";

        [MenuItem("Tools/江南染料/重新构建玩家动画 & 替换玩家")]
        public static void RebuildPlayerAnimation()
        {
            BuildClips();
            BuildController();
            ReplacePlayerInScene();
        }

        // ---------- 动画 Clip ----------
        private static void BuildClips()
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { WalkSpriteDir });
            Sprite[] frames = new Sprite[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[i]));
            System.Array.Sort(frames, (a, b) => NaturalCompare(a.name, b.name));
            Debug.Log("[PlayerAnimator] 找到 " + frames.Length + " 张走路帧");

            // 走路帧也强制对齐 PPU，确保和跑步帧一致
            EnsureFramesAligned(WalkSpriteDir, new Vector2(0.5f, 0.02f), 64);

            // 强制把跑步帧的 pivot 和 PPU 都设成和走路帧一致，避免切换动画时人物上下跳或大小变
            EnsureFramesAligned(RunSpriteDir, new Vector2(0.5f, 0.02f), 64);

            if (!AssetDatabase.IsValidFolder(AnimDir))
            {
                System.IO.Directory.CreateDirectory(AnimDir);
                AssetDatabase.Refresh();
            }

            EditorCurveBinding binding = new EditorCurveBinding
            {
                path = "Visual",
                propertyName = "m_Sprite",
                type = typeof(SpriteRenderer)
            };

            // Walk：9 帧循环，10 fps → 0.9s 一圈
            AnimationClip walk = new AnimationClip
            {
                name = "PlayerWalk",
                legacy = false,
                frameRate = 10f,
                wrapMode = WrapMode.Loop
            };
            ObjectReferenceKeyframe[] walkKeys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
                walkKeys[i] = new ObjectReferenceKeyframe { time = i / 10f, value = frames[i] };
            AnimationUtility.SetObjectReferenceCurve(walk, binding, walkKeys);
            Save(walk, AnimDir + "/PlayerWalk.anim");

            // Run：10 帧循环，15 fps → 0.67s 一圈
            string[] runGuids = AssetDatabase.FindAssets("t:Sprite", new[] { RunSpriteDir });
            Sprite[] runFrames = new Sprite[runGuids.Length];
            for (int i = 0; i < runGuids.Length; i++)
                runFrames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(runGuids[i]));
            System.Array.Sort(runFrames, (a, b) => NaturalCompare(a.name, b.name));
            Debug.Log("[PlayerAnimator] 找到 " + runFrames.Length + " 张跑步帧");

            AnimationClip run = new AnimationClip
            {
                name = "PlayerRun",
                legacy = false,
                frameRate = 15f,
                wrapMode = WrapMode.Loop
            };
            ObjectReferenceKeyframe[] runKeys = new ObjectReferenceKeyframe[runFrames.Length];
            for (int i = 0; i < runFrames.Length; i++)
                runKeys[i] = new ObjectReferenceKeyframe { time = i / 15f, value = runFrames[i] };
            AnimationUtility.SetObjectReferenceCurve(run, binding, runKeys);
            Save(run, AnimDir + "/PlayerRun.anim");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Idle：停在第 1 帧
            AnimationClip idle = new AnimationClip
            {
                name = "PlayerIdle",
                legacy = false,
                frameRate = 1f,
                wrapMode = WrapMode.ClampForever
            };
            ObjectReferenceKeyframe[] idleKeys =
            {
                new ObjectReferenceKeyframe { time = 0f, value = frames[0] }
            };
            AnimationUtility.SetObjectReferenceCurve(idle, binding, idleKeys);
            Save(idle, AnimDir + "/PlayerIdle.anim");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void Save(Object obj, string path)
        {
            Object old = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (old != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(obj, path);
        }

        // ---------- Animator Controller ----------
        private static void BuildController()
        {
            string path = AnimDir + "/Player.controller";
            if (System.IO.File.Exists(path)) AssetDatabase.DeleteAsset(path);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var sm = controller.layers[0].stateMachine;

            AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + "/PlayerIdle.anim");
            AnimationClip walk = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + "/PlayerWalk.anim");
            AnimationClip run = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + "/PlayerRun.anim");

            AnimatorState idleState = sm.AddState("Idle");
            idleState.motion = idle;
            sm.defaultState = idleState;

            AnimatorState walkState = sm.AddState("Walk");
            walkState.motion = walk;

            AnimatorState runState = sm.AddState("Run");
            runState.motion = run;

            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);

            // Idle → Walk
            AnimatorStateTransition t1 = idleState.AddTransition(walkState);
            t1.AddCondition(AnimatorConditionMode.If, 1f, "IsMoving");
            t1.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");
            t1.hasExitTime = false; t1.duration = 0.05f;

            // Idle → Run（直接起跑）
            AnimatorStateTransition t1b = idleState.AddTransition(runState);
            t1b.AddCondition(AnimatorConditionMode.If, 1f, "IsMoving");
            t1b.AddCondition(AnimatorConditionMode.If, 1f, "IsRunning");
            t1b.hasExitTime = false; t1b.duration = 0.05f;

            // Walk → Run
            AnimatorStateTransition t2 = walkState.AddTransition(runState);
            t2.AddCondition(AnimatorConditionMode.If, 1f, "IsRunning");
            t2.hasExitTime = false; t2.duration = 0.05f;

            // Run → Walk
            AnimatorStateTransition t3 = runState.AddTransition(walkState);
            t3.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsRunning");
            t3.hasExitTime = false; t3.duration = 0.05f;

            // Walk → Idle
            AnimatorStateTransition t4 = walkState.AddTransition(idleState);
            t4.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
            t4.hasExitTime = false; t4.duration = 0.05f;

            // Run → Idle
            AnimatorStateTransition t5 = runState.AddTransition(idleState);
            t5.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
            t5.hasExitTime = false; t5.duration = 0.05f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlayerAnimator] AnimatorController 已构建");
        }

        // ---------- 替换场景里的 Player ----------
        private static void ReplacePlayerInScene()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            var playerGo = GameObject.Find("Player");
            if (playerGo == null)
            {
                Debug.LogWarning("[PlayerAnimator] 场景中找不到 Player");
                return;
            }

            Sprite newIdleFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/a-walk/走路关键帧_01_三次修正.png");
            RuntimeAnimatorController animCtrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                AnimDir + "/Player.controller");

            // 找到 Visual 子物体
            Transform visual = playerGo.transform.Find("Visual");
            if (visual == null)
            {
                Debug.LogError("[PlayerAnimator] Player 下找不到 Visual 子物体");
                return;
            }

            SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = newIdleFrame;
                // 保留现有的 scale（之前已经是按 Player_4Dir 的高度算好的）
                // 但新帧尺寸和旧帧不同，重新算 scale 让玩家世界高度 ≈ 3.25
                float targetHeight = 3.25f;
                float frameHeightWorld = newIdleFrame.rect.height / newIdleFrame.pixelsPerUnit;
                float newScale = targetHeight / frameHeightWorld;
                visual.localScale = new Vector3(newScale, newScale, 1f);
                Debug.Log("[PlayerAnimator] 新帧 worldH=" + frameHeightWorld.ToString("F2") +
                          " → scale=" + newScale.ToString("F3"));
            }

            Animator anim = playerGo.GetComponent<Animator>();
            if (anim == null) anim = playerGo.AddComponent<Animator>();
            anim.runtimeAnimatorController = animCtrl;
            anim.updateMode = AnimatorUpdateMode.Normal;

            // 重新计算 CapsuleCollider2D：让 offset = Visual sprite bounds center，
            // 这样 InteractionDetector 的 collider.bounds.center 就是人物身体中心
            CapsuleCollider2D col = playerGo.GetComponent<CapsuleCollider2D>();
            if (col != null && sr != null)
            {
                // Visual 可视 bounds 的中心（考虑 Visual scale）
                Bounds b = sr.bounds;
                Vector3 scaledCenter = b.center;
                col.offset = new Vector2(scaledCenter.x - playerGo.transform.position.x,
                                         scaledCenter.y - playerGo.transform.position.y);
                // Collider 尺寸匹配可视范围的主要部分
                col.size = new Vector2(Mathf.Max(0.6f, b.size.x), Mathf.Max(1.6f, b.size.y * 0.8f));
                col.direction = CapsuleDirection2D.Vertical;
                Debug.Log("[PlayerAnimator] Collider: offset=" + col.offset + " size=" + col.size +
                          " bounds.center=" + col.bounds.center);
            }

            // 确保 PlayerController 里的引用都正确
            var pc = playerGo.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.animator = anim;
                pc.spriteRenderer = sr;
                pc.visual = visual;
            }

            // 同步更新 Player prefab
            string prefabPath = "Assets/4-寻找染料game/Game/Prefabs/Player/Player.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                var prefabVisual = prefab.transform.Find("Visual");
                var prefabSr = prefabVisual?.GetComponent<SpriteRenderer>();
                if (prefabSr != null) prefabSr.sprite = newIdleFrame;
                Animator prefabAnim = prefab.GetComponent<Animator>();
                if (prefabAnim == null) prefabAnim = prefab.AddComponent<Animator>();
                prefabAnim.runtimeAnimatorController = animCtrl;
                var prefabPc = prefab.GetComponent<PlayerController>();
                if (prefabPc != null) prefabPc.animator = prefabAnim;
                float targetHeight = 3.25f;
                float frameHeightWorld = newIdleFrame.rect.height / newIdleFrame.pixelsPerUnit;
                float newScale = targetHeight / frameHeightWorld;
                if (prefabVisual != null) prefabVisual.localScale = new Vector3(newScale, newScale, 1f);

                // Prefab 里的 collider 也要同步更新 offset 和 size
                CapsuleCollider2D prefabCol = prefab.GetComponent<CapsuleCollider2D>();
                if (prefabCol != null && prefabSr != null)
                {
                    Bounds pb = prefabSr.bounds;
                    prefabCol.offset = new Vector2(pb.center.x - prefab.transform.position.x,
                                                   pb.center.y - prefab.transform.position.y);
                    prefabCol.size = new Vector2(Mathf.Max(0.6f, pb.size.x), Mathf.Max(1.6f, pb.size.y * 0.8f));
                }
                if (prefabPc != null)
                {
                    prefabPc.animator = prefabAnim;
                    prefabPc.spriteRenderer = prefabSr;
                    prefabPc.visual = prefabVisual;
                }
                prefab = UnityEditor.PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                Debug.Log("[PlayerAnimator] Player prefab 已更新");
            }

            EditorUtility.SetDirty(playerGo);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("[PlayerAnimator] 场景 Player 替换完成");
        }

        // ---------- 辅助 ----------

        /// <summary>自然排序：把 "1" "2" "10" 按数字排，而不是字符串字典序。</summary>
        private static int NaturalCompare(string a, string b)
        {
            var regex = new System.Text.RegularExpressions.Regex(@"\d+");
            var ma = regex.Match(a);
            var mb = regex.Match(b);
            if (ma.Success && mb.Success)
            {
                int cmp = int.Parse(ma.Value).CompareTo(int.Parse(mb.Value));
                if (cmp != 0) return cmp;
            }
            return string.Compare(a, b, System.StringComparison.Ordinal);
        }

        /// <summary>强制把某文件夹下所有 Sprite 的 pivot 和 PPU 对齐，避免动画切换时人物上下跳或尺寸变。</summary>
        private static void EnsureFramesAligned(string folder, Vector2 pivotNormalized, int pixelsPerUnit)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;

                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                if (ti.spritePixelsPerUnit == pixelsPerUnit &&
                    settings.spriteAlignment == (int)SpriteAlignment.Custom &&
                    settings.spritePivot == pivotNormalized) continue;   // 已对齐，跳过

                ti.spritePixelsPerUnit = pixelsPerUnit;
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivotNormalized;
                ti.SetTextureSettings(settings);
                ti.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlayerAnimator] " + folder + " PPU→" + pixelsPerUnit + " pivot→(" +
                      pivotNormalized.x + ", " + pivotNormalized.y + ") 对齐完成");
        }
    }
}
#endif
