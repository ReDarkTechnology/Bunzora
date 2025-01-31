using System;
using System.Collections.Generic;
using System.Numerics;
using Eto;
using Raylib_CsLo;
using RayImGui;
using ImGuiNET;

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
        private bool WasHovered;

        private Vector2 PreviousPosition;
        private float PreviousPressure;
        private Vector2 ReportedStylusPosition = new Vector2(0, 0);
        private Vector2 ApproximateStylusPositionAtWindow;
        private float ReportedStylusPressure = 0;

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


            var pos = ApproximateStylusPositionAtWindow;
            if (IsStylusDown && !WasHovered)
            {
                if (WasUp)
                {
                    PreviousPosition = pos;
                    PreviousPressure = ReportedStylusPressure;
                    WasUp = false;
                }

                Draw(PreviousPosition, PreviousPressure, pos, ReportedStylusPressure);

                PreviousPosition = pos;
                PreviousPressure = ReportedStylusPressure;
            }
            else
            {
                if (!WasUp)
                {
                    RegisterStroke();
                    WasUp = true;
                }
                Raylib.DrawCircleV(pos, BrushSize, new Color(255, 0, 0, 255));
            }

            foreach (Layer layer in layers)
            {
                layer.Draw(0, 0);
            }

            rlImGui.Begin();
            ImGui.Begin("Brush Settings");
            ImGui.SliderFloat("Brush Size", ref BrushSize, 1, 1000);
            ImGui.ColorPicker4("Brush Color", ref BrushColor);
            ImGui.End();
            WasHovered = ImGui.IsAnyItemHovered();
            rlImGui.End();
            Raylib.EndDrawing();
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
