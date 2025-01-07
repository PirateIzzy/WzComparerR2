using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.WzLib;
using WzComparerR2.PluginBase;
using WzComparerR2.Rendering;
using WzComparerR2.Animation;

namespace WzComparerR2.AvatarCommon
{
    public class AvatarCanvasManager
    {
        public AvatarCanvasManager()
        {
            this.canvas = new AvatarCanvas();
            this.canvas.LoadZ();
            this.canvas.LoadActions();
            this.canvas.LoadEmotions();
        }

        private AvatarCanvas canvas;

        public void addBodyFromSkin3(int skin)
        {
            var a = $@"Character\00002{skin:D3}.img";
            Wz_Node bodyNode = PluginBase.PluginManager.FindWz($@"Character\00002{skin:D3}.img")
                        ?? PluginBase.PluginManager.FindWz($@"Character\00002000.img");
            Wz_Node headNode = PluginBase.PluginManager.FindWz($@"Character\00012{skin:D3}.img")
                ?? PluginBase.PluginManager.FindWz($@"Character\00012000.img");

            this.canvas.AddPart(bodyNode);
            this.canvas.AddPart(headNode);
        }

        public void addBodyFromSkin4(int skin)
        {
            Wz_Node bodyNode = PluginBase.PluginManager.FindWz($@"Character\0000{skin:D4}.img")
                        ?? PluginBase.PluginManager.FindWz($@"Character\00002000.img");
            Wz_Node headNode = PluginBase.PluginManager.FindWz($@"Character\0001{skin:D4}.img")
                ?? PluginBase.PluginManager.FindWz($@"Character\00012000.img");

            this.canvas.AddPart(bodyNode);
            this.canvas.AddPart(headNode);
        }

        public void addHairOrFace(int id)
        {
            var gearNode = PluginManager.FindWz($@"Character\Hair\{id:D8}.img") ??
                PluginManager.FindWz($@"Character\Face\{id:D8}.img");
            if (gearNode != null)
            {
                this.canvas.AddPart(gearNode);
            }
        }

        public void addGear(int id)
        {
            var gearNode = FindNodeByGearID(id);
            if (gearNode != null)
            {
                this.canvas.AddPart(gearNode);
            }
        }

        public void addGears(int[] ids)
        {
            foreach (var id in ids)
            {
                addGear(id);
            }
        }

        public BitmapOrigin getBitmapOrigin()
        {
            return getBitmapOrigin("stand1", "default", 0, 0, 0);
        }

        public BitmapOrigin getBitmapOrigin(string actionName, string emotionName, int bodyFrame, int faceFrame, int tamingFrame)
        {
            this.canvas.ActionName = actionName;
            this.canvas.EmotionName = emotionName;

            var bone = this.canvas.CreateFrame(bodyFrame, faceFrame, tamingFrame, null);
            var bitmapOrigin = this.canvas.DrawFrame(bone);

            return bitmapOrigin;
        }

        public Frame getTexture2DFrame(string actionName, string emotionName, int bodyFrame, int faceFrame, int tamingFrame, GraphicsDevice graphicsDevice)
        {
            var bitmapOrigin = getBitmapOrigin(actionName, emotionName, bodyFrame, faceFrame, tamingFrame);
            Texture2D texture = bitmapOrigin.Bitmap.ToTexture(graphicsDevice);

            Frame frame = new Frame(texture, new Point(bitmapOrigin.Origin.X, bitmapOrigin.Origin.Y), 0, getActionFrameDelay(actionName, bodyFrame), false);

            return frame;
        }

        private Wz_Node FindNodeByGearID(int id)
        {
            string imgName = id.ToString("D8") + ".img";
            Wz_Node imgNode = null;

            var characWz = PluginManager.FindWz(Wz_Type.Character);
            foreach (var node1 in characWz.Nodes)
            {
                if (node1.Text.Contains("_Canvas"))
                {
                    continue;
                }

                if (node1.Text == imgName)
                {
                    imgNode = node1;
                    break;
                }
                else if (node1.Nodes.Count > 0)
                {
                    foreach (var node2 in node1.Nodes)
                    {
                        if (node2.Text == imgName)
                        {
                            imgNode = node2;
                            break;
                        }
                    }
                    if (imgNode != null)
                    {
                        break;
                    }
                }
            }

            if (imgNode != null)
            {
                Wz_Image img = imgNode.GetValue<Wz_Image>();
                if (img != null && img.TryExtract())
                {
                    return img.Node;
                }
            }

            return null;
        }

        public int getActionFrameCount(string actionName)
        {
            Action action = this.canvas.Actions.Find(act => act.Name == actionName);
            if (action == null)
            {
                return 0;
            }

            Wz_Node node = PluginBase.PluginManager.FindWz("Character\\00002000.img");
            node = node?.FindNodeByPath(action.Name);
            if (node == null)
            {
                return 0;
            }

            return node.Nodes.Count;
        }

        public int getActionFrameDelay(string actionName, int bodyFrame)
        {
            Action action = this.canvas.Actions.Find(act => act.Name == actionName);
            if (action == null)
            {
                return 0;
            }

            Wz_Node node = PluginBase.PluginManager.FindWz("Character\\00002000.img");
            foreach (var path in new[] { action.Name, bodyFrame.ToString(), "delay" })
            {
                node = node?.FindNodeByPath(path);
                if (node == null)
                {
                    return 0;
                }
            }

            return node.GetValueEx<int>(0);
        }

        public void clearCanvas()
        {
            Array.Clear(this.canvas.Parts, 0, this.canvas.Parts.Length);
        }
    }
}
