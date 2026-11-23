using System;
using UnityEngine;

// Lightweight single-person silhouette estimate for a fixed, calibrated camera.
internal sealed class WorkshopWardrobeCameraTracking
{
    private const int Width = 80;
    private const int Height = 60;
    private const int SampleCount = Width * Height;
    private const int ColorThreshold = 34;

    private readonly Color32[] current = new Color32[SampleCount];
    private readonly Color32[] background = new Color32[SampleCount];
    private readonly bool[] foreground = new bool[SampleCount];
    private readonly int[] labels = new int[SampleCount];
    private readonly int[] queue = new int[SampleCount];
    private readonly int[] shoulderWidths = new int[Height];

    internal struct Pose
    {
        internal Vector2 Neck;
        internal Vector2 ShoulderMidpoint;
        internal float ShoulderWidth;
        internal float ShoulderSlope;
        internal bool HandRaised;
        internal float HandOffset;
    }

    internal void Sample(Color32[] source, int width, int height, bool verticallyMirrored, int quarterTurns = 0)
    {
        for (int y = 0; y < Height; y++)
        {
            float v = (y + 0.5f) / Height;
            for (int x = 0; x < Width; x++)
            {
                float u = (x + 0.5f) / Width;
                // Undo the display's horizontal mirror and clockwise rotation before sampling.
                float sourceU;
                float sourceV;
                switch (quarterTurns)
                {
                    case 1: sourceU = v; sourceV = u; break;
                    case 2: sourceU = u; sourceV = 1f - v; break;
                    case 3: sourceU = 1f - v; sourceV = 1f - u; break;
                    default: sourceU = 1f - u; sourceV = v; break;
                }
                int sourceX = (int)(sourceU * width);
                int sourceY = (int)(sourceV * height);
                if (!verticallyMirrored) sourceY = height - 1 - sourceY;
                current[y * Width + x] = source[sourceY * width + sourceX];
            }
        }
    }

    internal void CaptureBackground() => Array.Copy(current, background, SampleCount);

    internal bool TryEstimate(out Pose pose)
    {
        pose = default;
        ClassifyForeground();
        if (!FindTorso(out int label, out int top, out int bottom, out int center)) return false;

        int headTop = top;
        while (headTop < bottom && !TryReadRow(label, headTop, center, out _, out _)) headTop++;
        int height = bottom - headTop + 1;
        if (height < Height / 3) return false;

        int firstShoulder = headTop + Mathf.RoundToInt(height * 0.27f);
        int lastShoulder = headTop + Mathf.RoundToInt(height * 0.46f);
        int widthCount = 0;
        for (int y = firstShoulder; y <= lastShoulder; y++)
        {
            if (TryReadRow(label, y, center, out int left, out int right))
                shoulderWidths[widthCount++] = Mathf.Min(center - left, right - center) * 2 + 1;
        }
        if (widthCount == 0) return false;

        Array.Sort(shoulderWidths, 0, widthCount);
        int width = shoulderWidths[widthCount / 2];
        if (width < Width / 9 || width > Width * 0.68f || center - width / 2 < 0 || center + width / 2 >= Width)
            return false;

        // Sample the two sides of the shoulder contour outside the estimated neck.
        int half = Mathf.RoundToInt(width * 0.44f);
        int firstContour = headTop + Mathf.RoundToInt(height * 0.18f);
        int shoulderLeft = FirstContour(label, center - half, firstContour, lastShoulder);
        int shoulderRight = FirstContour(label, center + half, firstContour, lastShoulder);
        float shoulderY = (shoulderLeft + shoulderRight) * 0.5f;
        pose.ShoulderMidpoint = new Vector2((center + 0.5f) / Width, shoulderY / Height);
        pose.Neck = new Vector2((center + 0.5f) / Width, (shoulderY - height * 0.05f) / Height);
        pose.ShoulderWidth = (float)width / Width;
        pose.ShoulderSlope = (float)(shoulderRight - shoulderLeft) / Height;
        pose.HandRaised = TryFindRaisedHand(label, top, firstShoulder + 2, center, width, out pose.HandOffset);
        return true;
    }

    private static int Brightness(Color32 color) => (color.r + color.g + color.b) / 3;

    private void ClassifyForeground()
    {
        int brightnessShift = 0;
        for (int x = 0; x < Width; x++)
            brightnessShift += Brightness(current[x]) - Brightness(background[x]);
        brightnessShift = Mathf.Clamp(brightnessShift / Width, -24, 24);

        Array.Clear(labels, 0, SampleCount);
        for (int i = 0; i < SampleCount; i++)
        {
            Color32 pixel = current[i];
            Color32 original = background[i];
            int distance = Mathf.Abs(pixel.r - original.r - brightnessShift) +
                Mathf.Abs(pixel.g - original.g - brightnessShift) +
                Mathf.Abs(pixel.b - original.b - brightnessShift);
            foreground[i] = distance >= ColorThreshold * 3;
        }

        for (int y = 1; y < Height - 1; y++)
        {
            for (int x = 1; x < Width - 1; x++)
            {
                int i = y * Width + x;
                if (!foreground[i]) continue;
                // Reject isolated changes while keeping narrow connected arm silhouettes.
                int neighbors = (foreground[i - 1] ? 1 : 0) + (foreground[i + 1] ? 1 : 0) +
                    (foreground[i - Width] ? 1 : 0) + (foreground[i + Width] ? 1 : 0);
                if (neighbors >= 2) labels[i] = -1;
            }
        }
    }

    private bool FindTorso(out int label, out int top, out int bottom, out int center)
    {
        label = top = bottom = center = 0;
        int nextLabel = 0;
        int bestArea = 0;
        int secondArea = 0;
        for (int i = 0; i < SampleCount; i++)
        {
            if (labels[i] != -1) continue;
            int candidate = ++nextLabel;
            int area = 0;
            Enqueue(i, candidate, ref area);
            int minY = Height;
            int maxY = 0;
            int torsoX = 0;
            int torsoCount = 0;

            for (int j = 0; j < area; j++)
            {
                int index = queue[j];
                int x = index % Width;
                int y = index / Width;
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
                if (y >= Height * 0.60f)
                {
                    torsoX += x;
                    torsoCount++;
                }

                if (x > 0) Enqueue(index - 1, candidate, ref area);
                if (x + 1 < Width) Enqueue(index + 1, candidate, ref area);
                if (y > 0) Enqueue(index - Width, candidate, ref area);
                if (y + 1 < Height) Enqueue(index + Width, candidate, ref area);
            }

            if (area < SampleCount * 0.035f || area > SampleCount * 0.65f ||
                maxY - minY < Height / 3 || torsoCount == 0) continue;
            int candidateCenter = torsoX / torsoCount;
            if (candidateCenter < Width / 9 || candidateCenter > Width - Width / 9) continue;
            if (area <= bestArea)
            {
                secondArea = Mathf.Max(area, secondArea);
                continue;
            }

            secondArea = bestArea;
            bestArea = area;
            label = candidate;
            top = minY;
            bottom = maxY;
            center = candidateCenter;
        }
        return bestArea > 0 && secondArea < bestArea * 0.66f;
    }

    private void Enqueue(int index, int label, ref int count)
    {
        if (labels[index] != -1) return;
        labels[index] = label;
        queue[count++] = index;
    }

    private bool TryReadRow(int label, int y, int center, out int left, out int right)
    {
        int row = y * Width;
        left = right = center;
        if (labels[row + center] != label) return false;
        while (left > 0 && labels[row + left - 1] == label) left--;
        while (right + 1 < Width && labels[row + right + 1] == label) right++;
        return right - left >= 3;
    }

    private int FirstContour(int label, int x, int from, int to)
    {
        for (int y = from; y <= to; y++)
            if (labels[y * Width + x] == label) return y;
        return to;
    }

    private bool TryFindRaisedHand(int label, int top, int bottom, int center, int shoulderWidth, out float offset)
    {
        int sideBand = Mathf.CeilToInt(shoulderWidth * 0.56f);
        int leftSum = 0;
        int rightSum = 0;
        int leftCount = 0;
        int rightCount = 0;
        for (int y = top; y <= bottom; y++)
        {
            int row = y * Width;
            for (int x = 1; x < Width - 1; x++)
            {
                if (labels[row + x] != label) continue;
                if (x < center - sideBand)
                {
                    leftSum += x - center;
                    leftCount++;
                }
                else if (x > center + sideBand)
                {
                    rightSum += x - center;
                    rightCount++;
                }
            }
        }

        offset = 0f;
        const int minPixels = 10;
        bool left = leftCount >= minPixels;
        bool right = rightCount >= minPixels;
        if (left == right) return false;
        offset = (left ? (float)leftSum / leftCount : (float)rightSum / rightCount) / Width;
        return true;
    }
}
