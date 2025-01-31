using Raylib_CsLo;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Bunzora
{
    public class Layer
    {
        public string Name = "New Layer";
        public RenderTexture RenderTexture;

        public Layer(int width, int height) { RenderTexture = Raylib.LoadRenderTexture(width, height); }
        public Layer(string name, int width, int height) { Name = name; RenderTexture = Raylib.LoadRenderTexture(width, height); }
        public Layer(string name, RenderTexture renderTexture) { Name = name; RenderTexture = renderTexture; }

        public void Append(Action action)
        {
            Raylib.BeginTextureMode(RenderTexture);
            action?.Invoke();
            Raylib.EndTextureMode();
        }

        public void Blend(Layer layer)
        {
            Raylib.BeginTextureMode(RenderTexture);
            Raylib.DrawTexture(layer.RenderTexture.texture, 0, 0, new Color(255, 255, 255, 255));
            Raylib.EndTextureMode();
        }

        public void ReplaceAll(Action action) 
        { 
            Raylib.BeginTextureMode(RenderTexture); 
            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            action?.Invoke(); 
            Raylib.EndTextureMode(); 
        }

        public void Draw(int posX, int posY)
        {
            Raylib.DrawTexturePro(RenderTexture.texture, 
                new Rectangle(0, 0, RenderTexture.texture.width, -RenderTexture.texture.height),
                new Rectangle(posX, posY, RenderTexture.texture.width, RenderTexture.texture.height),
                new Vector2(0, 0),
                0,
                new Color(255, 255, 255, 255)
            );
        }

        public Image Backup() =>
            Raylib.LoadImageFromTexture(RenderTexture.texture);

        public void Restore(Image image)
        {
            var texture = Raylib.LoadTextureFromImage(image);
            ReplaceAll(() =>
            Raylib.DrawTexturePro(texture,
                new Rectangle(0, 0, texture.width, -texture.height),
                new Rectangle(0, 0, texture.width, texture.height),
                new Vector2(0, 0),
                0,
                new Color(255, 255, 255, 255)
            ));
        }
    }
}