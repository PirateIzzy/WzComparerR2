﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.WzLib;
using WzComparerR2.PluginBase;
using WzComparerR2.Common;
using WzComparerR2.MapRender.Patches2;
using WzComparerR2.MapRender.UI;
using Microsoft.Xna.Framework;
using IE = System.Collections.IEnumerator;
using WzComparerR2.CharaSim;
using WzComparerR2.Rendering;

namespace WzComparerR2.MapRender
{
    public partial class FrmMapRender2
    {
        enum SceneManagerState
        {
            Starting = 0,
            Loading,
            Entering,
            Running,
            Exiting,
        }

        #region private fields
        MapViewData viewData;
        LinkedList<MapViewData> viewHistory;
        SceneManagerState sceneManagerState;
        Wz_Image mapImgLoading;
        #endregion

        public void LoadMap(Wz_Image mapImg)
        {
            if (this.sceneManagerState == SceneManagerState.Starting || this.sceneManagerState == SceneManagerState.Running)
            {
                this.mapImgLoading = mapImg;
            }
        }

        /*
         * State machine transition table
         *
         * FromState |   condition  | ToState
         * ----------|--------------|-----------
         * Starting  | mapImg != null | Loading
         * Starting  | mapImg == null | Running (empty scene)
         * Loading   | mapData != null | Entering
         * Loading   | mapData == null | Running (empty scene)
         * Entering  | global_light >= 1.0 | Running
         * Running   | viewData.toMap != null | Exiting
         * Exiting  | viewData.toMap != null | Loading
         * 
         */

        private IE OnStart()
        {
            // initialize
            this.viewHistory = new LinkedList<MapViewData>();
            this.sceneManagerState = SceneManagerState.Starting;
            // add view state
            this.viewData = new MapViewData()
            {
                MapID = -1,
                ToMapID = null,
            };

            if (this.mapImgLoading != null)
            {
                this.viewData.ToPortal = "sp";
                yield return cm.Yield(OnMapLoading());
            }
            else
            {
                this.opacity = 1;
                yield return cm.Yield(OnSceneRunning());
            }
        }

        private IE OnMapLoading()
        {
            this.sceneManagerState = SceneManagerState.Loading;

            var loadMapTask = this.LoadMap();
            yield return new WaitTaskCompletedCoroutine(loadMapTask);
            if (loadMapTask.Exception != null)
            {
                this.ui.ChatBox.AppendTextSystem($"Failed to load map: {loadMapTask.Exception}");
                this.mapImgLoading = null;
                this.opacity = 1;
                yield return cm.Yield(OnSceneRunning());
                yield break;
            }

            // Backfill toMapID 
            if (this.viewData.ToMapID == null && this.mapData?.ID != null)
            {
                this.viewData.ToMapID = this.mapData.ID;
            }

            // Recording map history
            if (this.viewData.MapID > -1 && this.viewData.MapID != this.viewData.ToMapID && this.viewData.ToMapID != null)
            {
                if (this.viewData.IsMoveBack
                    && this.viewData.ToMapID == this.viewHistory.Last?.Value?.MapID)
                {
                    var last = this.viewHistory.Last.Value;
                    this.viewHistory.RemoveLast();
                    var toViewData = new MapViewData()
                    {
                        MapID = last.MapID,
                        Portal = last.Portal ?? "sp"
                    };
                    this.viewData = toViewData;
                }
                else
                {
                    viewHistory.AddLast(this.viewData);
                    var toViewData = new MapViewData()
                    {
                        MapID = this.viewData.ToMapID.Value,
                        Portal = this.viewData.ToPortal ?? "sp"
                    };
                    this.viewData = toViewData;
                }
            }
            else
            {
                this.viewData.MapID = this.viewData.ToMapID ?? -1;
                this.viewData.ToMapID = null;
                this.viewData.Portal = this.viewData.ToPortal;
                this.viewData.ToPortal = null;
            }

            this.viewData.IsMoveBack = false;
            yield return cm.Yield(OnSceneEnter());
        }

        private async Task<bool> LoadMap()
        {
            if (this.mapImgLoading == null)
            {
                return false;
            }

            // Start load
            this.resLoader.ClearAnimationCache();
            this.resLoader.BeginCounting();

            // Load map data
            var mapData = new MapData(this.Services.GetService<IRandom>());
            mapData.Load(this.mapImgLoading.Node, resLoader);

            // Load BGM
            Music newBgm = LoadBgm(mapData);
            Task bgmTask = null;
            bool willSwitchBgm = this.mapData?.Bgm != mapData.Bgm;
            if (willSwitchBgm && this.bgm != null) //准备切换
            {
                bgmTask = FadeOut(this.bgm, 1000);
            }

            // Load Resources
            mapData.PreloadResource(resLoader);

            // Preparing UI and initialization
            this.AfterLoadMap(mapData);

            if (bgmTask != null)
            {
                await bgmTask;
            }

            // Recycling
            this.resLoader.EndCounting();
            this.resLoader.Recycle();

            // Prepare scene and BGM
            this.mapImg = this.mapImgLoading;
            this.mapImgLoading = null;
            this.mapData = mapData;
            this.bgm = newBgm;
            if (willSwitchBgm && this.bgm != null)
            {
                bgmTask = FadeIn(this.bgm, 1000);
            }
            return true;
        }

        private async Task FadeOut(Music music, int ms)
        {
            float vol = music.Volume;
            for (int i = 0; i < ms; i += 30)
            {
                music.Volume = vol * (ms - i) / ms;
                await Task.Delay(30);
            }
            music.Volume = 0f;
            music.Stop();
        }

        private async Task FadeIn(Music music, int ms)
        {
            music.Play();
            float vol = music.Volume;
            for (int i = 0; i < ms; i += 30)
            {
                music.Volume = vol + (1 - vol) * i / ms;
                await Task.Delay(30);
            }
            music.Volume = 1f;
        }

        private Music LoadBgm(MapData mapData, string multiBgmText = null)
        {
            if (!string.IsNullOrEmpty(mapData.Bgm))
            {
                var path = new List<string>() { "Sound" };
                path.AddRange(mapData.Bgm.Split('/'));
                path[1] += ".img";
                var bgmNode = PluginManager.FindWz(string.Join("\\", path));
                if (bgmNode != null)
                {
                    if (bgmNode.Value == null)
                    {
                        bgmNode = multiBgmText == null ? bgmNode.Nodes.FirstOrDefault(n => n.Value is Wz_Sound || n.Value is Wz_Uol) : bgmNode.Nodes[multiBgmText];
                        if (bgmNode == null)
                        {
                            return null;
                        }
                    }

                    while (bgmNode.Value is Wz_Uol uol)
                    {
                        bgmNode = uol.HandleUol(bgmNode);
                    }
                    var bgm = resLoader.Load<Music>(bgmNode);
                    bgm.IsLoop = true;
                    return bgm;
                }
            }
            return null;
        }

        private Music LoadSoundEff(string path, bool useHolder = false)
        {
            var bgmNode = PluginManager.FindWz(path);
            if (bgmNode != null)
            {
                if (bgmNode.Value == null)
                {
                    bgmNode = bgmNode.Nodes.FirstOrDefault(n => n.Value is Wz_Sound || n.Value is Wz_Uol);
                    if (bgmNode == null)
                    {
                        return null;
                    }
                }

                while (bgmNode.Value is Wz_Uol uol)
                {
                    bgmNode = uol.HandleUol(bgmNode);
                }

                if (useHolder)
                {
                    var bgm = resLoader.Load<Music>(bgmNode);
                    bgm.IsLoop = false;
                    return bgm;
                }
                else
                {
                    Wz_Sound bgm = bgmNode.GetValue<Wz_Sound>();
                    Music sound = null;
                    if (bgm != null)
                    {
                        sound = new Music(bgm);
                    }

                    sound.IsLoop = false;
                    return sound;
                }
            }
            return null;
        }

        private void AfterLoadMap(MapData mapData)
        {
            // Synchronize visualization status
            foreach (var portal in mapData.Scene.Portals)
            {
                portal.View.IsEditorMode = this.patchVisibility.PortalInEditMode;
            }

            // Synchronous UI
            this.renderEnv.Camera.WorldRect = mapData.VRect;
            ResetCaptureRect();

            this.ui.MirrorFrame.Visibility = mapData.ID / 10000000 == 32 ? EmptyKeys.UserInterface.Visibility.Visible : EmptyKeys.UserInterface.Visibility.Collapsed;

            this.ui.Minimap.Mirror = mapData.ID / 10000000 == 32;

            // Disable OpenAI Translate Engine in MapRender
            bool isTranslateRequired = Translator.IsTranslateEnabled && Translator.DefaultPreferredTranslateEngine != 9;

            StringResult sr;
            if (mapData.ID != null && this.StringLinker != null
                && StringLinker.StringMap.TryGetValue(mapData.ID.Value, out sr))
            {
                this.ui.Minimap.StreetName = sr["streetName"];
                this.ui.Minimap.MapName = sr["mapName"];
                if (isTranslateRequired)
                {
                    this.ui.Minimap.StreetName = Translator.MergeString(sr["streetName"], Translator.TranslateString(sr["streetName"], true), 0, false, true);
                    this.ui.Minimap.MapName = Translator.MergeString(sr["mapName"], Translator.TranslateString(sr["mapName"], true), 0, false, true);
                }
            }
            else
            {
                this.ui.Minimap.StreetName = null;
                this.ui.Minimap.MapName = null;
            }

            if (mapData.MiniMap.MapMark != null)
            {
                this.ui.Minimap.MapMark = engine.Renderer.CreateTexture(mapData.MiniMap.MapMark);
            }
            else
            {
                this.ui.Minimap.MapMark = null;
            }

            if (mapData.MiniMap.Canvas != null)
            {
                this.ui.Minimap.MinimapCanvas = engine.Renderer.CreateTexture(mapData.MiniMap.Canvas);
            }
            else
            {
                this.ui.Minimap.MinimapCanvas = null;
            }

            this.ui.Minimap.Icons.Clear();
            foreach (var portal in mapData.Scene.Portals)
            {
                switch (portal.Type)
                {
                    case 2:
                    case 7:
                        object tooltip = portal.Tooltip;
                        if (tooltip == null && portal.ToMap != null && portal.ToMap != 999999999
                            && StringLinker.StringMap.TryGetValue(portal.ToMap.Value, out sr))
                        {
                            var spot = new UIWorldMap.MapSpot();
                            spot.Title = sr["mapName"];
                            spot.MapNo.Add(portal.ToMap ?? 0);
                            tooltip = new UIWorldMap.MapSpotTooltip() { Spot = spot };
                        }
                        this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                        {
                            IconType = portal.EnchantPortal ? UIMinimap2.IconType.EnchantPortal : UIMinimap2.IconType.Portal,
                            Tooltip = tooltip,
                            WorldPosition = new EmptyKeys.UserInterface.PointF(portal.X, portal.Y)
                        });
                        break;

                    case 8:
                        if (portal.ShownAtMinimap)
                        {
                            this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                            {
                                IconType = UIMinimap2.IconType.HiddenPortal,
                                Tooltip = portal.Tooltip,
                                WorldPosition = new EmptyKeys.UserInterface.PointF(portal.X, portal.Y)
                            });
                        }
                        break;

                    case 10:
                        this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                        {
                            IconType = (portal.ToMap == mapData.ID || (portal.ToMap == 999999999 && !string.IsNullOrEmpty(portal.ToName))) ? UIMinimap2.IconType.ArrowUp : UIMinimap2.IconType.HiddenPortal,
                            Tooltip = portal.Tooltip,
                            WorldPosition = new EmptyKeys.UserInterface.PointF(portal.X, portal.Y)
                        });
                        break;

                    case 11:
                        if (portal.ShownAtMinimap)
                        {
                            this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                            {
                                IconType = UIMinimap2.IconType.HiddenPortal,
                                Tooltip = portal.Tooltip,
                                WorldPosition = new EmptyKeys.UserInterface.PointF(portal.X, portal.Y)
                            });
                        }
                        break;
                }
            }
            foreach (var illuminantCluster in mapData.Scene.IlluminantClusters)
            {
                this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                {
                    IconType = UIMinimap2.IconType.HiddenPortal,
                    WorldPosition = new EmptyKeys.UserInterface.PointF(illuminantCluster.Start.X, illuminantCluster.Start.Y)
                });
            }

            foreach (var npc in mapData.Scene.Npcs)
            {
                object tooltip = null;
                var npcNode = PluginManager.FindWz(string.Format("Npc/{0:D7}.img/info", npc.ID));
                if ((npcNode?.Nodes["hide"].GetValueEx(0) ?? 0) != 0)
                {
                    continue;
                }
                if (StringLinker.StringNpc.TryGetValue(npc.ID, out sr))
                {
                    if (sr.Desc != null)
                    {
                        tooltip = new KeyValuePair<string, string>(sr.Name, sr.Desc);
                    }
                    else
                    {
                        tooltip = sr.Name;
                    }
                }
                if (npc.ID == 9010022 || (npcNode?.Nodes["miniMapType"].GetValueEx<int>(0) ?? 0) == 3)
                {
                    this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                    {
                        IconType = UIMinimap2.IconType.Transport,
                        Tooltip = tooltip,
                        WorldPosition = new EmptyKeys.UserInterface.PointF(npc.X, npc.Y)
                    });
                }
                else if ((npcNode?.Nodes["shop"].GetValueEx(0) ?? 0) != 0 || (npcNode?.Nodes["miniMapType"].GetValueEx<int>(0) ?? 0) == 1)
                {
                    this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                    {
                        IconType = UIMinimap2.IconType.Shop,
                        Tooltip = tooltip,
                        WorldPosition = new EmptyKeys.UserInterface.PointF(npc.X, npc.Y)
                    });
                }
                else if ((npcNode?.Nodes["miniMapType"].GetValueEx<int>(0) ?? 0) == 2)
                {
                    this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                    {
                        IconType = UIMinimap2.IconType.EventNpc,
                        Tooltip = tooltip,
                        WorldPosition = new EmptyKeys.UserInterface.PointF(npc.X, npc.Y)
                    });
                }
                else if ((npcNode?.Nodes["trunkPut"].GetValueEx(-1) ?? -1) != -1)
                {
                    this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                    {
                        IconType = UIMinimap2.IconType.Trunk,
                        Tooltip = tooltip,
                        WorldPosition = new EmptyKeys.UserInterface.PointF(npc.X, npc.Y)
                    });
                }
                else
                {
                    this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                    {
                        IconType = UIMinimap2.IconType.Npc,
                        Tooltip = tooltip,
                        WorldPosition = new EmptyKeys.UserInterface.PointF(npc.X, npc.Y)
                    });
                }
            }
            foreach (var mob in mapData.Scene.Mobs)
            {
                var mobNode = PluginManager.FindWz(string.Format("Mob/{0:D7}.img/info", mob.ID));
                if ((mobNode?.Nodes["minimap"].GetValueEx(0) ?? 0) != 0)
                {
                    this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                    {
                        IconType = UIMinimap2.IconType.Another,
                        WorldPosition = new EmptyKeys.UserInterface.PointF(mob.X, mob.Y)
                    });
                }
            }

            if (mapData.MiniMap.Width > 0 && mapData.MiniMap.Height > 0)
            {
                this.ui.Minimap.MapRegion = new Rectangle(-mapData.MiniMap.CenterX, -mapData.MiniMap.CenterY, mapData.MiniMap.Width, mapData.MiniMap.Height).ToRect();
            }
            else
            {
                this.ui.Minimap.MapRegion = mapData.VRect.ToRect();
            }

            this.ui.WorldMap.CurrentMapID = mapData?.ID;
        }

        private IE OnSceneEnter()
        {
            this.sceneManagerState = SceneManagerState.Entering;

            // Initialize portal
            if (!string.IsNullOrEmpty(viewData.Portal))
            {
                var portal = this.mapData.Scene.FindPortal(viewData.Portal);
                if (portal != null)
                {
                    this.renderEnv.Camera.Center = new Vector2(portal.X, portal.Y);
                }
                else
                {
                    this.renderEnv.Camera.Center = Vector2.Zero;
                }
                this.renderEnv.Camera.AdjustToWorldRect();
            }
            viewData.Portal = null;

            // Scene fading in
            this.opacity = 0;
            double time = 500;
            for (double i = 0; i < time; i += cm.GameTime.ElapsedGameTime.TotalMilliseconds)
            {
                this.opacity = (float)(i / time);
                SceneUpdate();
                yield return null;
            }
            this.opacity = 1;
            yield return cm.Yield(OnSceneRunning());
        }

        private IE OnSceneExit()
        {
            this.sceneManagerState = SceneManagerState.Exiting;

            // Scene fading out
            this.opacity = 1;
            double time = 500;
            for (double i = 0; i < time; i += cm.GameTime.ElapsedGameTime.TotalMilliseconds)
            {
                this.opacity = 1f - (float)(i / time);
                yield return null;
            }
            this.opacity = 0;
            yield return null;
            yield return cm.Yield(OnMapLoading());
        }

        private IE OnSceneRunning()
        {
            this.sceneManagerState = SceneManagerState.Running;
            while (true)
            {
                SceneUpdate();
                if (this.mapImgLoading != null)
                {
                    break;
                }
                yield return null;
            }
            yield return cm.Yield(OnSceneExit());
        }

        private IE OnCameraMoving(Point toPos, int ms)
        {
            Vector2 cameraFrom = this.renderEnv.Camera.Center;
            Vector2 cameraTo = toPos.ToVector2();
            for (double i = 0; i < ms; i += cm.GameTime.ElapsedGameTime.TotalMilliseconds)
            {
                var percent = (i / ms);
                this.renderEnv.Camera.Center = Vector2.Lerp(cameraFrom, cameraTo, (float)Math.Sqrt(percent));
                this.renderEnv.Camera.AdjustToWorldRect();
                yield return null;
            }
            this.renderEnv.Camera.Center = cameraTo;
            this.renderEnv.Camera.AdjustToWorldRect();
        }

        private async Task SetCameraChangedEffect(Vector2 pos)
        {
            if (this.mapData.ID / 100 == 9932670)
            {
                var bgmRegionsInfo = PluginManager.FindWz($@"Etc\MinigameClient.img\DimensionTower\fieldList\{this.mapData.ID}\bgmRegions");
                if (bgmRegionsInfo != null)
                {
                    var regionNode = bgmRegionsInfo.Nodes.FirstOrDefault(n =>
                    {
                        var lt = n.FindNodeByPath("lt").GetValueEx<Wz_Vector>(new Wz_Vector(0, 0)).ToPoint();
                        var rb = n.FindNodeByPath("rb").GetValueEx<Wz_Vector>(new Wz_Vector(0, 0)).ToPoint();

                        if (pos.X >= lt.X && pos.X <= rb.X && pos.Y >= lt.Y && pos.Y <= rb.Y)
                        {
                            return true;
                        }
                        return false;
                    });

                    string bgm;
                    if (regionNode != null)
                    {
                        bgm = regionNode.FindNodeByPath("bgm").GetValueEx<string>("Bgm00/Silence");
                    }
                    else
                    {
                        bgm = "Bgm00/Silence";
                    }

                    if (!string.IsNullOrEmpty(bgm))
                    {
                        bool willSwitchBgm = this.mapData.Bgm != bgm;
                        this.mapData.Bgm = bgm;
                        Music newBgm = LoadBgm(this.mapData);
                        if (newBgm != null)
                        {
                            Task bgmTask = null;
                            if (willSwitchBgm && this.bgm != null) //准备切换
                            {
                                bgmTask = FadeOut(this.bgm, 500);
                            }

                            if (bgmTask != null)
                            {
                                await bgmTask;
                            }

                            this.bgm = newBgm;
                            if (willSwitchBgm && this.bgm != null)
                            {
                                bgmTask = FadeIn(this.bgm, 500);
                            }

                            if (bgmTask != null)
                            {
                                await bgmTask;
                            }
                        }
                    }
                }
            }
            this.CamaraChangedEffState = true;
        }

        private void SceneUpdate()
        {
            var gameTime = cm.GameTime;
            var mapData = this.mapData;

            if (this.IsActive)
            {
                this.renderEnv.Input.Update(gameTime);
                this.ui.UpdateInput(gameTime.ElapsedGameTime.TotalMilliseconds);
            }

            // Manual data update required
            this.renderEnv.Camera.AdjustToWorldRect();
            {
                var rect = this.renderEnv.Camera.ClipRect;
                this.ui.Minimap.CameraViewPort = new EmptyKeys.UserInterface.Rect(rect.X, rect.Y, rect.Width, rect.Height);
            }
            // Update topbar
            UpdateTopBar();
            // Update UI
            this.ui.UpdateLayout(gameTime.ElapsedGameTime.TotalMilliseconds);
            // Update scene
            if (mapData != null)
            {
                UpdateAllItems(mapData.Scene, gameTime.ElapsedGameTime);
            }
            // Update tooltip
            UpdateTooltip();
        }

        private void MoveToPortal(int? toMap, string pName, string fromPName = null, bool isBack = false)
        {
            if (toMap != null && toMap != this.mapData?.ID) // Move to map
            {
                // Find map data
                Wz_Node node;
                if (MapData.FindMapByID(toMap.Value, out node))
                {
                    Wz_Image img = node.GetNodeWzImage();
                    if (img != null)
                    {
                        this.mapImgLoading = img;
                        viewData.ToMapID = toMap;
                        viewData.ToPortal = pName;
                        viewData.Portal = fromPName;
                        viewData.IsMoveBack = isBack;
                    }
                }
                else
                {
                    this.ui.ChatBox.AppendTextSystem($"You cannot move to map {toMap.Value}.");
                }
            }
            else // Current map
            {
                BlinkPortal(pName);
            }
        }

        private void BlinkPortal(string pName)
        {
            viewData.ToMapID = null;
            viewData.ToPortal = null;
            var portal = this.mapData.Scene.FindPortal(pName);
            if (portal != null)
            {
                this.cm.StartCoroutine(OnCameraMoving(new Point(portal.X, portal.Y), 500));
            }
        }

        private void MoveToLastMap()
        {
            if (viewHistory.Count > 0)
            {
                var last = viewHistory.Last.Value;
                if (last.MapID > -1)
                {
                    MoveToPortal(last.MapID, last.Portal, null, true);
                }
            }
        }

        class MapViewData
        {
            public int MapID { get; set; }
            public string Portal { get; set; }
            public int? ToMapID { get; set; }
            public string ToPortal { get; set; }
            public bool IsMoveBack { get; set; }
        }
    }
}
