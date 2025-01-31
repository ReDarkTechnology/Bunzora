using System;
using System.Collections.Generic;
using System.Numerics;
using Eto;
using Raylib_CsLo;
using RayImGui;
using ImGuiNET;
using Bunzora.Smoothing;

namespace Bunzora
{
    public class MainScene : IDisposable
    {
        public static Platform Platform { get; private set; }
        public static MainScene Instance { get; private set; }
        public static Stylus stylus;

        public event Action OnPreEventPoll;
        public event Action OnDisposed;

        public List<Layer> layers = new List<Layer>();

        public List<LastStrokeEvent> lastStrokeHistory = new List<LastStrokeEvent>();
        public LastStrokeEvent lastLastStrokeEvent;

        public Layer selectedLayer;
        public float BrushSize = 10f;
        public Vector4 BrushColor = new Vector4(0, 0, 0, 1f);
        private Color RBrushColor => new Color((int)(BrushColor.X * 255), (int)(BrushColor.Y * 255), (int)(BrushColor.Z * 255), (int)(BrushColor.W * 255));
        private bool IsStylusDown;
        private bool WasUp = true;
        private bool WasHoveringAtDown;
        private bool WasHovered;

        public float FillTolerance = 10f;
        public int GapThreshold = 5;

        private Vector2 PreviousPosition;
        private float PreviousPressure;
        private Vector2 ReportedStylusPosition = new Vector2(0, 0);
        private Vector2 ApproximateStylusPositionAtWindow;
        private float ReportedStylusPressure = 0;

        public ISmoothing smoothing = new ExponentialMovingAverage();
        public int smoothingIntensity = 50;

        public MainScene(int width = 960, int height = 540, string title = "Bunzora")
        {
            Instance = this;

            Raylib.SetConfigFlags(ConfigFlags.FLAG_VSYNC_HINT | ConfigFlags.FLAG_WINDOW_RESIZABLE | ConfigFlags.FLAG_MSAA_4X_HINT | ConfigFlags.FLAG_INTERLACED_HINT);
            Raylib.InitWindow(width, height, title);
            
            stylus = Stylus.CreateStylus();
            unsafe { stylus.Hook((IntPtr)Raylib.GetWindowHandle()); }

            stylus.OnStylusDown += (pos, pressure) => { IsStylusDown = true; Console.WriteLine($"Stylus down: P:{pos}, R:{pressure}"); OnStylusUpdate(pos, pressure); };
            stylus.OnStylusMove += (pos, pressure) => { OnStylusUpdate(pos, pressure); };
            stylus.OnStylusUp += () => { IsStylusDown = false; Console.WriteLine("Stylus up"); };

            selectedLayer = new Layer(Raylib.GetRenderWidth(), Raylib.GetRenderHeight());
            layers.Add(selectedLayer);

            RegisterStroke();
            rlImGui.Setup(true);
        }

        public void OnStylusUpdate(Vector2 position, float pressure)
        {
            ReportedStylusPosition = position;
            ReportedStylusPressure = pressure;
            ApproximateStylusPositionAtWindow = ReportedStylusPosition - Raylib.GetWindowPosition();
        }

        public void Update()
        {
            if (Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL))
            {
                if (Raylib.IsKeyPressed(KeyboardKey.KEY_Z))
                {
                    Undo();
                    Console.WriteLine("Undo");
                }

                if (Raylib.IsKeyPressed(KeyboardKey.KEY_Y) || ((Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT)) && Raylib.IsKeyPressed(KeyboardKey.KEY_Z)))
                {
                    Redo();
                    Console.WriteLine("Redo");
                }
            }

            if (Raylib.IsWindowReady())
                OnPreEventPoll?.Invoke();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(255, 255, 255, 255));

            var pos = smoothing.Stroke(ApproximateStylusPositionAtWindow, smoothingIntensity, 5);
            if (IsStylusDown)
            {
                if (WasUp)
                {
                    WasHoveringAtDown = WasHovered;
                    WasUp = false;

                    OnStylusDown(pos);
                }

                OnStylusPressed(pos);
            }
            else
            {
                if (!WasUp)
                {
                    OnStylusUp();
                    WasUp = true;
                }
                Raylib.DrawCircleV(pos, BrushSize, new Color(255, 0, 0, 255));
            }

            foreach (Layer layer in layers)
            {
                layer.Draw(0, 0);
            }

            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_RIGHT))
            {
                BucketFill();
            }

            rlImGui.Begin();
            WasHovered = ImGui.IsAnyItemHovered() || ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);
            ImGui.Begin("Brush Settings");
            ImGui.SliderFloat("Brush Size", ref BrushSize, 1, 1000);
            ImGui.SliderInt("Brush Smoothing", ref smoothingIntensity, 0, 100);
            ImGui.ColorPicker4("Brush Color", ref BrushColor);
            ImGui.Text($"Brush Color: {WasHovered}");
            ImGui.End();
            rlImGui.End();
            Raylib.EndDrawing();
        }

        public void OnStylusDown(Vector2 position)
        {
            if (WasHoveringAtDown) return;

            PreviousPosition = position;
            PreviousPressure = ReportedStylusPressure;
        }

        public void OnStylusPressed(Vector2 position)
        {
            if (WasHoveringAtDown) return;

            Draw(PreviousPosition, PreviousPressure, position, ReportedStylusPressure);

            PreviousPosition = position;
            PreviousPressure = ReportedStylusPressure;
        }

        public void OnStylusUp()
        {
            if (WasHoveringAtDown) return;

            RegisterStroke();
        }

        public void BucketFill()
        {
            var width = selectedLayer.RenderTexture.texture.width;
            var height = selectedLayer.RenderTexture.texture.height;

            Vector2 mousePos = Raylib.GetMousePosition();
            int x = (int)mousePos.X;
            int y = height - (int)mousePos.Y; // Invert Y position
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                unsafe
                {
                    Image img = Raylib.LoadImageFromTexture(selectedLayer.RenderTexture.texture);
                    Color* pixels = Raylib.LoadImageColors(img);
                    Color targetColor = pixels[y * width + x];

                    if (!ColorsAreSimilar(targetColor, RBrushColor, FillTolerance))
                    {
                        FloodFill(pixels, x, y, targetColor, RBrushColor, width, height, FillTolerance, GapThreshold);
                        Raylib.UpdateTexture(selectedLayer.RenderTexture.texture, pixels);
                    }

                    Raylib.UnloadImageColors(pixels);
                    Raylib.UnloadImage(img);
                }
                RegisterStroke();
            }
        }

        static bool ColorsAreSimilar(Color a, Color b, float tolerance)
        {
            float toleranceSq = tolerance * tolerance;

            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;

            return (dr * dr + dg * dg + db * db) <= toleranceSq;
        }

        unsafe static void FloodFill(Color* pixels, int x, int y, Color target, Color replacement, int width, int height, float colorTolerance, int gapThreshold)
        {
            if (ColorsAreSimilar(target, replacement, colorTolerance)) return;

            Queue<(int, int)> queue = new Queue<(int, int)>();
            HashSet<(int, int)> visited = new HashSet<(int, int)>();

            queue.Enqueue((x, y));
            visited.Add((x, y));

            while (queue.Count > 0)
            {
                (int cx, int cy) = queue.Dequeue();
                int index = cy * width + cx;

                if (!ColorsAreSimilar(pixels[index], target, colorTolerance))
                    continue;

                pixels[index] = replacement;

                // 4-way flood fill with optional gap handling
                TryEnqueue(cx + 1, cy);
                TryEnqueue(cx - 1, cy);
                TryEnqueue(cx, cy + 1);
                TryEnqueue(cx, cy - 1);

                if (gapThreshold > 0)
                {
                    for (int dx = -gapThreshold; dx <= gapThreshold; dx++)
                    {
                        for (int dy = -gapThreshold; dy <= gapThreshold; dy++)
                        {
                            if (dx == 0 && dy == 0) continue; // Skip self
                            TryEnqueue(cx + dx, cy + dy);
                        }
                    }
                }
            }

            void TryEnqueue(int nx, int ny)
            {
                if (nx >= 0 && nx < width && ny >= 0 && ny < height && !visited.Contains((nx, ny)))
                {
                    int neighborIndex = ny * width + nx;
                    if (ColorsAreSimilar(pixels[neighborIndex], target, colorTolerance))
                    {
                        queue.Enqueue((nx, ny));
                        visited.Add((nx, ny));
                    }
                }
            }
        }

        public void RegisterStroke()
        {
            var index = lastStrokeHistory.IndexOf(lastLastStrokeEvent);
            if (index < lastStrokeHistory.Count - 1)
            {
                for (int i = index + 1; i < lastStrokeHistory.Count; i++)
                    Raylib.UnloadImage(lastStrokeHistory[i].image);
                lastStrokeHistory.RemoveRange(index + 1, lastStrokeHistory.Count - index - 1);
            }

            lastLastStrokeEvent = new LastStrokeEvent() {
                image = selectedLayer.Backup(),
                layer = selectedLayer
            };

            lastStrokeHistory.Add(lastLastStrokeEvent);
        }

        public void Undo()
        {
            var index = lastStrokeHistory.IndexOf(lastLastStrokeEvent);
            if (index > 0)
            {
                lastLastStrokeEvent = lastStrokeHistory[index - 1];
                selectedLayer = lastLastStrokeEvent.layer;

                selectedLayer.Restore(lastLastStrokeEvent.image);
            }
        }

        public void Redo()
        {
            var index = lastStrokeHistory.IndexOf(lastLastStrokeEvent);
            if (index < lastStrokeHistory.Count - 1)
            {
                lastLastStrokeEvent = lastStrokeHistory[index + 1];
                selectedLayer = lastLastStrokeEvent.layer;

                selectedLayer.Restore(lastLastStrokeEvent.image);
            }
        }

        public void Draw(Vector2 sPos, float sPress, Vector2 ePos, float ePress)
        {
            Vector2 direction = Vector2.Normalize(ePos - sPos);
            float distance = Vector2.Distance(ePos, sPos);
            float stepSize = 1.0f;

            for (float i = 0; i <= distance; i += stepSize)
            {
                float t = i / distance;
                Vector2 interpolatedPos = Vector2.Lerp(sPos, ePos, t);
                float interpolatedRadius = sPress + (ePress - sPress) * t;

                selectedLayer.Append(() => Raylib.DrawCircleV(interpolatedPos, interpolatedRadius * BrushSize, RBrushColor));
            }

            // Preview the current cursor...
            Raylib.DrawCircleV(ePos, ePress * BrushSize, RBrushColor);
        }

        public bool ShouldClose()
        {
            return Raylib.WindowShouldClose();
        }

        public void Dispose()
        {
            Raylib.CloseWindow();
            OnDisposed?.Invoke();
        }

        public static MainScene Create(string platform, int width, int height, string title)
        {
            //Platform = Platform.Get(platform);
            Instance = new MainScene(width, height, title);
            return Instance;
        }

        public void RunAndWait()
        {
            while (!ShouldClose())
            {
                Update();
            }
            Dispose();
        }

        public class StrokeEvent
        {
            public Vector2 position;
            public float pressure;
            public Layer layer;
        }

        public class LastStrokeEvent
        {
            public Image image;
            public Layer layer;
        }
    }
}
