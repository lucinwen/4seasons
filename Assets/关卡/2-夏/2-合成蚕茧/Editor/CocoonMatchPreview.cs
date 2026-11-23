using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ShiXu.CocoonMatch.Editor
{
    public static class CocoonMatchPreview
    {
        // Render a disposable preview scene; do not enter Play or edit the authored scene.
        public static string Capture()
        {
            return CaptureView("Start");
        }

        public static string CaptureBoard() => CaptureView("Board");
        public static string CaptureHarvest() => CaptureView("Harvest");
        public static string CaptureResult() => CaptureView("Result");

        private static string CaptureView(string view)
        {
            var source = UnityEngine.Object.FindObjectOfType<CocoonMatchController>();
            if (source == null || Application.isPlaying) throw new InvalidOperationException("请在小游戏的编辑模式下预览。");
            var previewScene = EditorSceneManager.NewPreviewScene();
            var previousActive = RenderTexture.active;
            RenderTexture target = null;
            Texture2D image = null;
            try
            {
                var clone = UnityEngine.Object.Instantiate(source.gameObject);
                SceneManager.MoveGameObjectToScene(clone, previewScene);
                var controller = clone.GetComponent<CocoonMatchController>();
                controller.startPanel.SetActive(view == "Start");
                controller.resultPanel.SetActive(view == "Result");
                controller.closedPanel.SetActive(false);
                if (view == "Result")
                {
                    controller.resultTitle.text = "收茧圆满";
                    controller.resultMessage.text = "芒种收茧，寸丝成锦。\n全部蚕茧与工具都已收好！";
                }
                if (view == "Harvest")
                {
                    controller.harvestText.text = controller.level.patterns[0].harvestMessage;
                    controller.harvestGroup.alpha = 1;
                }
                var canvas = clone.GetComponent<Canvas>();
                var cameraObject = new GameObject("PreviewCamera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, previewScene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.scene = previewScene;
                camera.transform.position = new Vector3(0, 0, -100);
                camera.orthographic = true;
                camera.orthographicSize = 500;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                target = new RenderTexture(1600, 1000, 24);
                target.Create();
                camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 10;
                var scaler = clone.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                image = new Texture2D(1600, 1000, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
                image.Apply();
                Directory.CreateDirectory("CocoonMatchSupport/Previews");
                string path = Path.GetFullPath("CocoonMatchSupport/Previews/Mangzhong" + view + ".png");
                File.WriteAllBytes(path, image.EncodeToPNG());
                return path;
            }
            finally
            {
                RenderTexture.active = previousActive;
                EditorSceneManager.ClosePreviewScene(previewScene);
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            }
        }
    }
}
