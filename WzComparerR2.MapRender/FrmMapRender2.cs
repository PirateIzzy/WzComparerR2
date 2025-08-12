﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.MapRender.Config;
using WzComparerR2.MapRender.Patches2;
using WzComparerR2.MapRender.UI;
using WzComparerR2.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Form = System.Windows.Forms.Form;
using JLChnToZ.IMEHelper;

#region USING_EK
using EmptyKeys.UserInterface;
using KeyBinding = EmptyKeys.UserInterface.Input.KeyBinding;
using RelayCommand = EmptyKeys.UserInterface.Input.RelayCommand;
using KeyCode = EmptyKeys.UserInterface.Input.KeyCode;
using ModifierKeys = EmptyKeys.UserInterface.Input.ModifierKeys;
using ServiceManager = EmptyKeys.UserInterface.Mvvm.ServiceManager;
using WzComparerR2.MapRender.Effects;
#endregion

namespace WzComparerR2.MapRender
{
    public partial class FrmMapRender2 : Game
    {
        public FrmMapRender2()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.DeviceCreated += Graphics_DeviceCreated;
            graphics.DeviceResetting += Graphics_DeviceResetting;
            graphics.GraphicsProfile = GraphicsProfile.HiDef;

            this.MaxElapsedTime = TimeSpan.MaxValue;
            this.IsFixedTimeStep = false;
            this.TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60);
            this.InactiveSleepTime = TimeSpan.FromSeconds(1.0 / 30);
            this.IsMouseVisible = true;
            this.Exiting += (o, e) => this.OnExiting();

            this.Content = new WcR2ContentManager(this.Services);
            this.patchVisibility = new PatchVisibility();
            this.patchVisibility.FootHoldVisible = false;
            this.patchVisibility.LadderRopeVisible = false;
            this.patchVisibility.SkyWhaleVisible = false;
            this.patchVisibility.IlluminantClusterPathVisible = false;
            this.patchVisibility.SpringPortalPathVisible = false;
            this.patchVisibility.PortalRangeVisible = false;
            this.patchVisibility.ObstacleAreaVisible = false;
            this.patchVisibility.MobHitboxVisible = false;
            this.patchVisibility.CaptureRectVisible = false;

            var form = Form.FromHandle(this.Window.Handle) as Form;
            form.Load += Form_Load;
            form.GotFocus += Form_GotFocus;
            form.LostFocus += Form_LostFocus;
            form.FormClosing += Form_FormClosing;
            form.FormClosed += Form_FormClosed;

            this.imeHelper = new IMEHandler(this, true);
            this.Exiting += (o, e) => this.imeHelper.Dispose();
            this.Disposed += (o, e) => this.imeHelper.Dispose();
            GameExt.FixKeyboard(this);
        }

        private void Form_Load(object sender, EventArgs e)
        {
            var form = (Form)sender;
            form.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            form.SetDesktopLocation(0, 0);
        }

        private void Form_GotFocus(object sender, EventArgs e)
        {
            if (MapRenderConfig.Default.MuteOnLeaveFocus)
            {
                if (this.bgm != null)
                {
                    this.bgm.Volume = 1;
                }
            }
        }

        private void Form_LostFocus(object sender, EventArgs e)
        {
            if (MapRenderConfig.Default.MuteOnLeaveFocus)
            {
                if (this.bgm != null)
                {
                    this.bgm.Volume = 0;
                }
            }
        }
        private void Form_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            GameExt.ReleaseKeyboard(this);
        }

        private void Form_FormClosed(object sender, System.Windows.Forms.FormClosedEventArgs e)
        {
            GameExt.EnsureGameExit(this);
        }

        private void Graphics_DeviceCreated(object sender, EventArgs e)
        {
            this.engine = new WcR2Engine(this.GraphicsDevice,
                graphics.PreferredBackBufferWidth,
                graphics.PreferredBackBufferHeight);

            WcR2Engine.FixEKBugs();

            (this.engine.AssetManager as WcR2AssetManager).DefaultContentManager = this.Content as WcR2ContentManager;
        }

        private void Graphics_DeviceResetting(object sender, EventArgs e)
        {
            // fix DXGI error 0x887A0001 on gamewindow resizing
            WzComparerR2.Rendering.D2DFactory.Instance.ReleaseContext(graphics.GraphicsDevice);
        }

        public StringLinker StringLinker { get; set; }

        GraphicsDeviceManager graphics;
        Wz_Image mapImg;
        RenderEnv renderEnv;
        MapData mapData;
        ResourceLoader resLoader;
        MeshBatcher batcher;
        LightRenderer lightRenderer;
        PatchVisibility patchVisibility;

        bool prepareCapture;
        bool captureViewPortOnly;
        bool ForceCaptureWithResolution;
        Task captureTask;
        Resolution resolution;
        float opacity;

        List<ItemRect> allItems = new List<ItemRect>();
        List<KeyValuePair<SceneItem, MeshItem>> drawableItemsCache = new List<KeyValuePair<SceneItem, MeshItem>>();
        MapRenderUIRoot ui;
        Tooltip2 tooltip;
        WcR2Engine engine;
        Music bgm;

        CoroutineManager cm;
        FpsCounter fpsCounter;
        readonly List<IDisposable> attachedEvent = new List<IDisposable>();
        IMEHandler imeHelper;

        bool isUnloaded;
        bool isExiting;

        bool CamaraChangedEffState = true;
        Rectangle CaptureRect = new Rectangle();

        protected override void Initialize()
        {

            //init services
            this.Services.AddService<Random>(new Random());
            this.Services.AddService<IRandom>(new ParticleRandom(this.Services.GetService<Random>()));
            this.Services.AddService<IMEHandler>(this.imeHelper);

            ServiceManager.Instance.AddService<IMEHandler>(this.imeHelper);

            //init components
            this.renderEnv = new RenderEnv(this, this.graphics);
            this.batcher = new MeshBatcher(this.GraphicsDevice) { CullingEnabled = true };
            this.lightRenderer = new LightRenderer(this.GraphicsDevice);
            this.resLoader = new ResourceLoader(this.Services);
            this.resLoader.PatchVisibility = this.patchVisibility;
            this.ui = new MapRenderUIRoot();
            this.ui.Minimap.uiRoot = this.ui;
            this.ui.Minimap.mapRender2Root = this;
            this.BindingUIInput();
            this.tooltip = new Tooltip2(this.Content);
            this.tooltip.StringLinker = this.StringLinker;
            this.cm = new CoroutineManager(this);
            this.cm.StartCoroutine(OnStart()); //entry
            this.Components.Add(cm);
            this.fpsCounter = new FpsCounter(this) { UseStopwatch = true };
            this.Components.Add(fpsCounter);

            this.ApplySetting();
            SwitchResolution(Resolution.Window_1366_768);
            base.Initialize();

            //init UI teleport
            this.ui.Teleport.Sl = this.StringLinker;
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            base.OnDeactivated(sender, args);
        }

        private void BindingUIInput()
        {
            //键盘事件
            //切换分辨率
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => SwitchResolution()), KeyCode.Enter, ModifierKeys.Alt));

            //开关小地图
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.ui.Minimap.Toggle()), KeyCode.M, ModifierKeys.None) { IsRepeatEnabled = true });
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.ui.WorldMap.Toggle()), KeyCode.W, ModifierKeys.None) { IsRepeatEnabled = true });

            //选项界面
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ =>
            {
                var uiWnd = this.ui.Windows.OfType<UIOptions>().FirstOrDefault();
                if (uiWnd == null)
                {
                    uiWnd = new UIOptions();
                    uiWnd.DataContext = new UIOptionsDataModel();
                    uiWnd.OK += UIOption_OK;
                    uiWnd.Cancel += UIOption_Cancel;
                    uiWnd.ResetSCRect += UIOption_ResetSCRect;
                    uiWnd.ChkForceClickEvent += UIOption_ChkForceClickEvent;
                    uiWnd.Visible += UiWnd_Visible;
                    uiWnd.Visibility = EmptyKeys.UserInterface.Visibility.Visible;
                    this.ui.Windows.Add(uiWnd);
                    uiWnd.Parent = this.ui;
                }
                else
                {
                    uiWnd.Toggle();
                }
            }), KeyCode.Escape, ModifierKeys.None));

            //截图
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => { if (CanCapture()) prepareCapture = true; captureViewPortOnly = false; }), KeyCode.Scroll, ModifierKeys.None));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => { if (CanCapture()) prepareCapture = true; captureViewPortOnly = true; }), KeyCode.S, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.patchVisibility.CaptureRectVisible = !this.patchVisibility.CaptureRectVisible), KeyCode.S, ModifierKeys.None));

            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => { renderEnv.Camera.AdjustRectEnabled = !renderEnv.Camera.AdjustRectEnabled; }), KeyCode.U, ModifierKeys.Control));

            //层隐藏
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.patchVisibility.BackVisible = !this.patchVisibility.BackVisible), KeyCode.D1, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.patchVisibility.ReactorVisible = !this.patchVisibility.ReactorVisible), KeyCode.D2, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.patchVisibility.ObjVisible = !this.patchVisibility.ObjVisible), KeyCode.D3, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.patchVisibility.TileVisible = !this.patchVisibility.TileVisible), KeyCode.D4, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.patchVisibility.NpcVisible = !this.patchVisibility.NpcVisible), KeyCode.D5, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ =>
            {
                if (!this.patchVisibility.MobVisible)
                {
                    this.patchVisibility.MobVisible = true;
                }
                else if (!this.patchVisibility.MobHitboxVisible)
                {
                    this.patchVisibility.MobHitboxVisible = true;
                }
                else
                {
                    this.patchVisibility.MobVisible = false;
                    this.patchVisibility.MobHitboxVisible = false;
                }
            }), KeyCode.D6, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ =>
            {
                var visible = this.patchVisibility.FootHoldVisible;
                this.patchVisibility.FootHoldVisible = !visible;
                this.patchVisibility.LadderRopeVisible = !visible;
                this.patchVisibility.SkyWhaleVisible = !visible;
                this.patchVisibility.IlluminantClusterPathVisible = !visible;
                this.patchVisibility.SpringPortalPathVisible = !visible;
                this.patchVisibility.PortalRangeVisible = !visible;
                this.patchVisibility.ObstacleAreaVisible = !visible;
            }), KeyCode.D7, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ =>
            {
                var portals = this.mapData.Scene.Portals;
                if (!this.patchVisibility.PortalVisible)
                {
                    this.patchVisibility.PortalVisible = true;
                    this.patchVisibility.PortalInEditMode = false;
                    foreach (var item in portals)
                    {
                        item.View.IsEditorMode = false;
                    }
                    this.patchVisibility.IlluminantClusterVisible = true;
                }
                else if (!this.patchVisibility.PortalInEditMode)
                {
                    this.patchVisibility.PortalInEditMode = true;
                    foreach (var item in portals)
                    {
                        item.View.IsEditorMode = true;
                    }
                    this.patchVisibility.IlluminantClusterVisible = false;
                }
                else
                {
                    this.patchVisibility.PortalVisible = false;
                }
            }), KeyCode.D8, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.patchVisibility.FrontVisible = !this.patchVisibility.FrontVisible), KeyCode.D9, ModifierKeys.Control));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.patchVisibility.EffectVisible = !this.patchVisibility.EffectVisible), KeyCode.D0, ModifierKeys.Control));
            //移动操作
            #region 移动操作
            {
                //键盘移动
                int boostMoveFlag = 0;
                var direction1 = Vector2.Zero;

                Action<Vector2> calcKeyboardMoveDir = dir =>
                {
                    if (dir.X != 0)
                    {
                        var preMove = dir.X * 10 * (boostMoveFlag != 0 ? 3 : 1);

                        if (Math.Sign(preMove) * Math.Sign(direction1.X) == -1
                            && Math.Abs(direction1.X) <= Math.Abs(preMove))
                        {
                            direction1.X = 0;
                        }
                        else
                        {
                            direction1.X += preMove;
                        }
                    }
                    if (dir.Y != 0)
                    {
                        var preMove = dir.Y * 10 * (boostMoveFlag != 0 ? 3 : 1);

                        if (Math.Sign(preMove) * Math.Sign(direction1.Y) == -1
                            && Math.Abs(direction1.Y) <= Math.Abs(preMove))
                        {
                            direction1.Y = 0;
                        }
                        else
                        {
                            direction1.Y += preMove;
                        }
                    }
                };

                //键盘动量减速
                Action keyboardMoveSlowDown = () =>
                {
                    if (direction1.X > 2 || direction1.X < -2) direction1.X *= 0.98f;
                    else direction1.X = 0;
                    if (direction1.Y > 2 || direction1.Y < 2) direction1.Y *= 0.98f;
                    else direction1.Y = 0;
                };

                EmptyKeys.UserInterface.Input.KeyEventHandler keyEv;
                var keyApplied = new Dictionary<KeyCode, bool>();

                keyEv = (o, e) =>
                {
                    if (EmptyKeys.UserInterface.Input.InputManager.Current.FocusedElement is EmptyKeys.UserInterface.Controls.Primitives.TextBoxBase)
                    {
                        return;
                    }

                    switch (e.Key)
                    {
                        case KeyCode.Up:
                            calcKeyboardMoveDir(new Vector2(0, -1));
                            break;
                        case KeyCode.Down:
                            calcKeyboardMoveDir(new Vector2(0, 1));
                            break;
                        case KeyCode.Left:
                            calcKeyboardMoveDir(new Vector2(-1, 0));
                            break;
                        case KeyCode.Right:
                            calcKeyboardMoveDir(new Vector2(1, 0));
                            break;

                        case KeyCode.LeftControl:
                            boostMoveFlag |= 0x01;
                            break;
                        case KeyCode.RightControl:
                            boostMoveFlag |= 0x02;
                            break;

                        default:
                            return;
                    }
                    keyApplied[e.Key] = true;
                    e.Handled = true;
                };
                this.ui.PreviewKeyDown += keyEv;
                this.attachedEvent.Add(EventDisposable(keyEv, _ev => this.ui.PreviewKeyDown -= _ev));

                keyEv = (o, e) =>
                {
                    switch (e.Key)
                    {
                        case KeyCode.Up:
                        case KeyCode.Down:
                        case KeyCode.Left:
                        case KeyCode.Right:
                            if (keyApplied.TryGetValue(e.Key, out bool pressed) && pressed)
                            {
                                keyApplied[e.Key] = false;
                                e.Handled = true;
                            }
                            break;

                        case KeyCode.LeftControl:
                            boostMoveFlag &= ~0x01;
                            break;
                        case KeyCode.RightControl:
                            boostMoveFlag &= ~0x02;
                            break;
                    }
                };
                this.ui.PreviewKeyUp += keyEv;
                this.attachedEvent.Add(EventDisposable(keyEv, _ev => this.ui.PreviewKeyUp -= _ev));

                //鼠标移动
                bool isMouseDown = false;
                var direction2 = Vector2.Zero;
                int captureRectClickedPos = -1;
                Point prevMousePos = Point.Zero;

                Action<EmptyKeys.UserInterface.Input.MouseEventArgs> calcMouseMoveDir = e =>
                {
                    var mousePos = e.GetPosition();
                    Vector2 vec = new Vector2(2 * mousePos.X / this.ui.Width - 1, 2 * mousePos.Y / this.ui.Height - 1);
                    var distance = vec.Length();
                    if (distance >= 0.4f)
                    {
                        vec *= (distance - 0.4f) / distance;
                    }
                    else
                    {
                        vec = Vector2.Zero;
                    }
                    direction2 = vec * 20;
                };

                EmptyKeys.UserInterface.Input.MouseEventHandler mouseEv;
                EmptyKeys.UserInterface.Input.MouseButtonEventHandler mouseBtnEv;

                mouseBtnEv = (o, e) =>
                {
                    if (e.ChangedButton == EmptyKeys.UserInterface.Input.MouseButton.Right)
                    {
                        isMouseDown = true;
                        calcMouseMoveDir(e);
                    }
                    else if (e.ChangedButton == EmptyKeys.UserInterface.Input.MouseButton.Left)
                    {
                        if (this.patchVisibility.CaptureRectVisible)
                        {
                            Rectangle rect = this.renderEnv.Camera.WorldRect;
                            if (!this.CaptureRect.IsEmpty)
                            {
                                rect = this.CaptureRect;
                            }

                            //  1 | 5 | 2
                            //  8 | 0 | 6
                            //  4 | 7 | 3
                            var mouse = this.renderEnv.Input.MousePosition;
                            var mousePos = this.renderEnv.Camera.CameraToWorld(mouse);
                            int x = mousePos.X;
                            int y = mousePos.Y;
                            int inner = 15;
                            int outer = 10;
                            int pos = -1;
                            if (rect.Left - outer <= x && x < rect.Left + inner && rect.Top - outer <= y && y < rect.Top + inner)
                            {
                                pos = 1;
                            }
                            else if (rect.Right - inner <= x && x < rect.Right + outer && rect.Top - outer <= y && y < rect.Top + inner)
                            {
                                pos = 2;
                            }
                            else if (rect.Right - inner <= x && x < rect.Right + outer && rect.Bottom - inner <= y && y < rect.Bottom + outer)
                            {
                                pos = 3;
                            }
                            else if (rect.Left - outer <= x && x < rect.Left + inner && rect.Bottom - inner <= y && y < rect.Bottom + outer)
                            {
                                pos = 4;
                            }
                            else if (rect.Left - outer <= x && x < rect.Right + outer && rect.Top - outer <= y && y < rect.Top + inner)
                            {
                                pos = 5;
                            }
                            else if (rect.Right - inner <= x && x < rect.Right + outer && rect.Top - outer <= y && y < rect.Bottom + outer)
                            {
                                pos = 6;
                            }
                            else if (rect.Left - outer <= x && x < rect.Right + outer && rect.Bottom - inner <= y && y < rect.Bottom + outer)
                            {
                                pos = 7;
                            }
                            else if (rect.Left - outer <= x && x < rect.Left + inner && rect.Top - outer <= y && y < rect.Bottom + outer)
                            {
                                pos = 8;
                            }
                            else if (rect.Contains(x, y))
                            {
                                pos = 0;
                            }

                            captureRectClickedPos = pos;
                            prevMousePos = mousePos;
                        }
                    }
                };
                this.ui.MouseDown += mouseBtnEv;
                this.attachedEvent.Add(EventDisposable(mouseBtnEv, _ev => this.ui.MouseDown -= _ev));

                mouseEv = (o, e) =>
                {
                    if (isMouseDown)
                    {
                        calcMouseMoveDir(e);
                    }
                    if (captureRectClickedPos >= 0)
                    {
                        var mouse = this.renderEnv.Input.MousePosition;
                        var mousePos = this.renderEnv.Camera.CameraToWorld(mouse);
                        var dx = mousePos.X - prevMousePos.X;
                        var dy = mousePos.Y - prevMousePos.Y;
                        var minX = 25;
                        var minY = 25;
                        Rectangle rect = this.renderEnv.Camera.WorldRect;
                        if (!this.CaptureRect.IsEmpty)
                        {
                            rect = this.CaptureRect;
                        }

                        if (captureRectClickedPos == 0)
                        {
                            rect.X += dx;
                            rect.Y += dy;
                        }
                        if (captureRectClickedPos == 1 || captureRectClickedPos == 4 || captureRectClickedPos == 8) // L
                        {
                            var pdx = dx;
                            rect.Width -= dx;
                            if (rect.Width < minX)
                            {
                                pdx -= Math.Max(0, minX - rect.Width);
                                rect.Width = minX;
                            }
                            rect.X += pdx;
                        }
                        if (captureRectClickedPos == 1 || captureRectClickedPos == 2 || captureRectClickedPos == 5) // T
                        {
                            var pdy = dy;
                            rect.Height -= dy;
                            if (rect.Height < minY)
                            {
                                pdy -= Math.Max(0, minY - rect.Height);
                                rect.Height = minY;
                            }
                            rect.Y += pdy;
                        }
                        if (captureRectClickedPos == 2 || captureRectClickedPos == 3 || captureRectClickedPos == 6) // R
                        {
                            rect.Width += dx;
                        }
                        if (captureRectClickedPos == 3 || captureRectClickedPos == 4 || captureRectClickedPos == 7) // B
                        {
                            rect.Height += dy;
                        }

                        rect.Width = Math.Max(minX, rect.Width);
                        rect.Height = Math.Max(minY, rect.Height);
                        this.CaptureRect = rect;
                        var uiWnd = this.ui.Windows.OfType<UIOptions>().FirstOrDefault();
                        if (uiWnd != null) LoadCaptureRectOptionData(uiWnd.DataContext as UIOptionsDataModel);
                        prevMousePos = mousePos;
                    }
                };
                this.ui.MouseMove += mouseEv;
                this.attachedEvent.Add(EventDisposable(mouseEv, _ev => this.ui.MouseMove -= _ev));

                mouseBtnEv = (o, e) =>
                {
                    if (e.ChangedButton == EmptyKeys.UserInterface.Input.MouseButton.Right)
                    {
                        isMouseDown = false;
                        direction2 = Vector2.Zero;
                    }
                    else if (e.ChangedButton == EmptyKeys.UserInterface.Input.MouseButton.Left)
                    {
                        captureRectClickedPos = -1;
                        prevMousePos = Point.Zero;
                    }
                };
                this.ui.MouseUp += mouseBtnEv;
                this.attachedEvent.Add(EventDisposable(mouseBtnEv, _ev => this.ui.MouseUp -= _ev));

                //更新事件
                EventHandler ev = async (o, e) =>
                {
                    this.renderEnv.Camera.Center += direction1 + direction2 * ((boostMoveFlag != 0) ? 3 : 1);
                    keyboardMoveSlowDown();
                    if (this.CamaraChangedEffState)
                    {
                        this.CamaraChangedEffState = false;
                        await SetCameraChangedEffect(this.renderEnv.Camera.Center);
                    }
                };
                this.ui.InputUpdated += ev;
                this.attachedEvent.Add(EventDisposable(ev, _ev => this.ui.InputUpdated -= _ev));
            }
            #endregion

            //点击事件
            var disposable = UIHelper.RegisterClickEvent<SceneItem>(this.ui.ContentControl,
                (sender, point) =>
                {
                    int x = (int)point.X;
                    int y = (int)point.Y;
                    var mouseTarget = this.allItems.Reverse<ItemRect>().FirstOrDefault(item =>
                    {
                        return item.rect.Contains(x, y) && (item.item is PortalItem || item.item is IlluminantClusterItem || item.item is ReactorItem || item.item is LifeItem);
                    });
                    return mouseTarget.item;
                },
                this.OnSceneItemClick);
            this.attachedEvent.Add(disposable);

            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => {
                if (this.ui.Visibility == Visibility.Visible)
                {
                    this.ui.ChatBox.TextBoxChat.Focus();
                }
            }), KeyCode.Enter, ModifierKeys.None));
            this.ui.InputBindings.Add(new KeyBinding(new RelayCommand(_ => this.ui.ChatBox.Toggle()), KeyCode.Oem3, ModifierKeys.None));
            this.ui.WorldMap.MapSpotClick += WorldMap_MapSpotClick;
            this.ui.Teleport.SelectedMapGo += Teleport_SelectedMapGo;
            this.ui.ChatBox.TextBoxChat.TextSubmit += ChatBox_TextSubmit;
        }

        private void UIOption_OK(object sender, EventArgs e)
        {
            var wnd = sender as UIOptions;
            var data = wnd.DataContext as UIOptionsDataModel;
            SaveOptionData(data);
            wnd.Hide();

            ApplySetting();
        }

        private void UIOption_Cancel(object sender, EventArgs e)
        {
            var wnd = sender as UIOptions;
            wnd.Hide();
        }
        
        private void UIOption_ResetSCRect(object sender, EventArgs e)
        {
            var wnd = sender as UIOptions;
            var data = wnd.DataContext as UIOptionsDataModel;
            ResetCaptureRect();
            LoadCaptureRectOptionData(data);
        }

        private void UIOption_ChkForceClickEvent(object sender, EventArgs e)
        {
            var wnd = sender as UIOptions;
            var data = wnd.DataContext as UIOptionsDataModel;
            this.ForceCaptureWithResolution = data.ForceCaptureWithResolution;
            if (data.ForceCaptureWithResolution)
            {
                ResetCaptureRect();
                LoadCaptureRectOptionData(data);
            }
        }
        
        private void UiWnd_Visible(object sender, RoutedEventArgs e)
        {
            var wnd = sender as UIOptions;
            var data = wnd.DataContext as UIOptionsDataModel;
            LoadOptionData(data);
        }

        private void WorldMap_MapSpotClick(object sender, UIWorldMap.MapSpotEventArgs e)
        {
            int mapID = e.MapID;

            var callback = new EmptyKeys.UserInterface.Input.RelayCommand<MessageBoxResult>(r =>
            {
                if (r == MessageBoxResult.OK)
                {
                    this.MoveToPortal(mapID, "sp");
                }
            });

            StringResult sr = null;
            this.StringLinker?.StringMap.TryGetValue(mapID, out sr);
            //string mapName = sr?["mapName"] ?? "(null)"; //Kenny ver.
            //int last = (mapName.LastOrDefault(c => c >= '가' && c <= '힣') - '가') % 28; //Kenny ver.
            var message = string.Format("Will you teleport to this map?\r\n{0} ({1})", sr?.Name ?? "null", mapID);
            //var message = mapName + (last == 0 || last == 8 ? "" : "으") + "로 이동하시겠습니까?"; //Kenny ver.
            MessageBox.Show(message, "", MessageBoxButton.OKCancel, callback, false);
        }

        private void Teleport_SelectedMapGo(object sender, UITeleport.SelectedMapGoEventArgs e)
        {
            int mapID = e.MapID;
            this.MoveToPortal(mapID, "sp");
        }

        private void ChatBox_TextSubmit(object sender, TextEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Text))
            {
                if (e.Text.StartsWith("/"))
                {
                    ChatCommand(e.Text);
                }
                else
                {
                    this.ui.ChatBox.AppendTextNormal("MapRender: " + e.Text);
                }
            }
        }

        public async void ChatCommand(string command)
        {
            string[] arguments = command.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (arguments.Length <= 0)
            {
                return;
            }

            switch (arguments[0].ToLower())
            {
                case "/help":
                case "/?":
                    this.ui.ChatBox.AppendTextHelp(@"/help Show available commands");
                    this.ui.ChatBox.AppendTextHelp(@"/map (mapID) Teleport to a specified map");
                    this.ui.ChatBox.AppendTextHelp(@"/back Return to the previous map");
                    this.ui.ChatBox.AppendTextHelp(@"/home Teleport to the town of the region you are in");
                    this.ui.ChatBox.AppendTextHelp(@"/history [maxCount] History of visited maps");
                    this.ui.ChatBox.AppendTextHelp(@"/minimap Mini map settings");
                    this.ui.ChatBox.AppendTextHelp(@"/scene Scene settings");
                    this.ui.ChatBox.AppendTextHelp(@"/quest Quest settings");
                    this.ui.ChatBox.AppendTextHelp(@"/questex Set Quest key value");
                    this.ui.ChatBox.AppendTextHelp(@"/date Date settings");
                    this.ui.ChatBox.AppendTextHelp(@"/multibgm Multi BGM settings");
                    break;

                case "/map":
                    int toMapID;
                    if (arguments.Length > 1 && Int32.TryParse(arguments[1], out toMapID) && toMapID > -1)
                    {
                        this.MoveToPortal(toMapID, "sp");
                    }
                    else
                    {
                        this.ui.ChatBox.AppendTextSystem($"Please enter a correct map ID.");
                    }
                    break;

                case "/return":
                    if (this.viewHistory.Count > 0)
                    {
                        this.MoveToLastMap();
                    }
                    else
                    {
                        this.ui.ChatBox.AppendTextSystem($"There is no previous map to return to.");
                    }
                    break;

                case "/home":
                    var retMapID = this.mapData?.ReturnMap;
                    if (retMapID == null || retMapID == 999999999)
                    {
                        this.ui.ChatBox.AppendTextSystem($"You cannot return to town.");
                    }
                    else
                    {
                        this.MoveToPortal(retMapID, "sp");
                    }
                    break;

                case "/history":
                    int historyCount;
                    if (!(arguments.Length > 1
                        && Int32.TryParse(arguments[1], out historyCount)
                        && historyCount > 0))
                    {
                        historyCount = 5;
                    }
                    this.ui.ChatBox.AppendTextHelp($"Number of visited maps: ({this.viewHistory.Count})");
                    var node = this.viewHistory.Last;
                    while (node != null && historyCount > 0)
                    {
                        StringResult sr = null;
                        if (node.Value != null && this.StringLinker != null)
                        {
                            this.StringLinker.StringMap.TryGetValue(node.Value.MapID, out sr);
                        }
                        this.ui.ChatBox.AppendTextHelp($"  {sr?.Name ?? "(null)"}({node.Value.MapID})");

                        node = node.Previous;
                        historyCount--;
                    }
                    break;
                    
                case "/minimap":
                    var canvasList = this.mapData?.MiniMap?.ExtraCanvas;
                    switch (arguments.ElementAtOrDefault(1))
                    {
                        case "list":
                            this.ui.ChatBox.AppendTextHelp($"Mini map: {string.Join(", ", canvasList?.Keys)}");
                            break;

                        case "set":
                            string canvasName = arguments.ElementAtOrDefault(2);
                            if (canvasList != null && canvasList.TryGetValue(canvasName, out Texture2D canvas))
                            {
                                this.ui.Minimap.MinimapCanvas = engine.Renderer.CreateTexture(canvas);
                                this.ui.ChatBox.AppendTextHelp($"Mini map change completed: {canvasName}");
                            }
                            else
                            {
                                this.ui.ChatBox.AppendTextSystem($"Mini map not found: {canvasName}");
                            }
                            break;

                        default:
                            this.ui.ChatBox.AppendTextHelp(@"/minimap list View list of mini maps");
                            this.ui.ChatBox.AppendTextHelp(@"/minimap set (canvasName) Change the corresponding mini map");
                            break;
                    }
                    break;

                case "/scene":
                    switch (arguments.ElementAtOrDefault(1))
                    {
                        case "tag":
                            switch (arguments.ElementAtOrDefault(2))
                            {
                                case "list":
                                    var mapTags = GetSceneContainers(this.mapData?.Scene)
                                        .SelectMany(container => container.Slots)
                                        .Where(sceneItem => sceneItem.Tags != null && sceneItem.Tags.Length > 0)
                                        .SelectMany(sceneItem => sceneItem.Tags)
                                        .Distinct()
                                        .OrderBy(tag => tag)
                                        .ToList();
                                    this.ui.ChatBox.AppendTextHelp($"Tag list: {string.Join(", ", mapTags)}");
                                    break;
                                case "info":
                                    var visibleTags = this.patchVisibility.TagsVisible.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
                                    var hiddenTags = this.patchVisibility.TagsVisible.Where(kv => !kv.Value).Select(kv => kv.Key).ToList();
                                    this.ui.ChatBox.AppendTextHelp($"Default tag display state: {this.patchVisibility.DefaultTagVisible}");
                                    this.ui.ChatBox.AppendTextHelp($"Shown tag: {string.Join(", ", visibleTags)}");
                                    this.ui.ChatBox.AppendTextHelp($"Hidden tag: {string.Join(", ", hiddenTags)}");
                                    break;
                                case "show":
                                    string[] tags = arguments.Skip(3).ToArray();
                                    if (tags.Length > 0)
                                    {
                                        foreach (var tag in tags)
                                        {
                                            this.patchVisibility.SetTagVisible(tag, true);
                                        }
                                        this.ui.ChatBox.AppendTextHelp($"Completed tag: {string.Join(", ", tags)}");
                                    }
                                    else
                                    {
                                        this.ui.ChatBox.AppendTextSystem("Please enter a tag.");
                                    }
                                    break;
                                case "hide":
                                    tags = arguments.Skip(3).ToArray();
                                    if (tags.Length > 0)
                                    {
                                        foreach (var tag in tags)
                                        {
                                            this.patchVisibility.SetTagVisible(tag, false);
                                        }
                                        this.ui.ChatBox.AppendTextHelp($"Completed tag: {string.Join(", ", tags)}");
                                    }
                                    else
                                    {
                                        this.ui.ChatBox.AppendTextSystem("Please enter a tag.");
                                    }
                                    break;
                                case "reset":
                                    tags = arguments.Skip(3).ToArray();
                                    if (tags.Length > 0)
                                    {
                                        this.patchVisibility.ResetTagVisible(tags);
                                        this.ui.ChatBox.AppendTextHelp($"Completed tag display state reset: {string.Join(", ", tags)}");
                                    }
                                    else
                                    {
                                        this.ui.ChatBox.AppendTextSystem("Please enter a tag.");
                                    }
                                    break;
                                case "reset-all":
                                    this.patchVisibility.ResetTagVisible();
                                    this.ui.ChatBox.AppendTextHelp($"Completed all tag display state reset");
                                    break;
                                case "set-default":
                                    if (bool.TryParse(arguments.ElementAtOrDefault(3), out bool isVisible))
                                    {
                                        this.patchVisibility.DefaultTagVisible = isVisible;
                                        this.ui.ChatBox.AppendTextHelp($"Completed tag default display state: {isVisible}");
                                    }
                                    else
                                    {
                                        this.ui.ChatBox.AppendTextSystem("Please enter a correct value.");
                                    }
                                    break;
                                default:
                                    this.ui.ChatBox.AppendTextHelp(@"/scene tag list View a list of tags");
                                    this.ui.ChatBox.AppendTextHelp(@"/scene tag info View current tag display status");
                                    this.ui.ChatBox.AppendTextHelp(@"/scene tag show (tagName)... Show tag visibility");
                                    this.ui.ChatBox.AppendTextHelp(@"/scene tag hide (tagName)... Hide tag visibility");
                                    this.ui.ChatBox.AppendTextHelp(@"/scene tag reset (tagName)... Reset tag");
                                    this.ui.ChatBox.AppendTextHelp(@"/scene tag reset-all Reset all tag visibility");
                                    this.ui.ChatBox.AppendTextHelp(@"/scene tag set-default (true/false) Reset tag default display state");
                                    break;
                            }
                            break;

                        default:
                            this.ui.ChatBox.AppendTextHelp(@"/scene tag Tag display state settings");
                            break;
                    }
                    break;



                case "/date":
                    switch (arguments.ElementAtOrDefault(1))
                    {
                        case "list":
                            List<Tuple<long, long>> dateList = this?.mapData.Scene.Npcs.SelectMany(item => item.Date).ToList();
                            this.ui.ChatBox.AppendTextHelp($"Related dates: ({dateList.Count()})");
                            foreach (Tuple<long, long> item in dateList)
                            {
                                this.ui.ChatBox.AppendTextHelp($"  {item.Item1} - {item.Item2}");
                            }
                            break;

                        case "set":
                            if (DateTime.TryParseExact(arguments.ElementAtOrDefault(2), "yyyyMMddHHmm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime datetime))
                            {
                                this.mapData.Date = datetime;
                                this.mapData.PreloadResource(resLoader);
                                this.ui.ChatBox.AppendTextSystem($"Changed rendering base time to {datetime}.");
                            }
                            else
                            {
                                this.ui.ChatBox.AppendTextSystem($"Please enter a correct time and date.");
                            }
                            break;

                        default:
                            this.ui.ChatBox.AppendTextHelp(@"/date list View a list of related dates");
                            this.ui.ChatBox.AppendTextHelp(@"/date set (yyyyMMddHHmm) Time and date reference settings");
                            break;
                    }
                    break;

                case "/multibgm":
                    switch (arguments.ElementAtOrDefault(1))
                    {
                        case "list":
                            if (!string.IsNullOrEmpty(this.mapData.Bgm))
                            {
                                var path = new List<string>() { "Sound" };
                                path.AddRange(this.mapData.Bgm.Split('/'));
                                path[1] += ".img";
                                var bgmNode = PluginBase.PluginManager.FindWz(string.Join("\\", path));
                                var subNodes = bgmNode?.Nodes ?? new Wz_Node.WzNodeCollection(null);
                                this.ui.ChatBox.AppendTextHelp($"Multi BGM: {subNodes.Count}");
                                foreach (Wz_Node subNode in subNodes)
                                {
                                    this.ui.ChatBox.AppendTextHelp($"  {subNode.Text}");
                                }
                            }
                            else
                            {
                                this.ui.ChatBox.AppendTextHelp($"Multi BGM: 0");
                            }
                            break;

                        case "set":
                            string bgmName = string.Join(" ", arguments.Skip(2));
                            Music multiBgm = LoadBgm(this.mapData, bgmName);
                            if (multiBgm != null)
                            {
                                this.ui.ChatBox.AppendTextSystem($"Changed the Multi BGM to {bgmName}.");

                                Task bgmTask = null;
                                bool willSwitchBgm = this.bgm != multiBgm;
                                if (willSwitchBgm && this.bgm != null) //准备切换
                                {
                                    bgmTask = FadeOut(this.bgm, 1000);
                                }

                                if (bgmTask != null)
                                {
                                    await bgmTask;
                                }

                                this.bgm = multiBgm;
                                if (willSwitchBgm && this.bgm != null)
                                {
                                    bgmTask = FadeIn(this.bgm, 1000);
                                }
                            }
                            else
                            {
                                this.ui.ChatBox.AppendTextHelp($"Please enter a correct Multi BGM.");
                            }
                            break;

                        default:
                            this.ui.ChatBox.AppendTextHelp(@"/multibgm list View list of Multi BGM(s)");
                            this.ui.ChatBox.AppendTextHelp(@"/multibgm set (multiBgm) Plays the corresponding Multi BGM");
                            break;
                    }
                    break;

                case "/quest":
                    switch (arguments.ElementAtOrDefault(1))
                    {
                        case "list":
                            List<QuestInfo> questList = this?.mapData.Scene.Back.Slots.SelectMany(item => ((BackItem)item).Quest)
                                .Concat(this?.mapData.Scene.Layers.Nodes.SelectMany(l => ((LayerNode)l).Obj.Slots.SelectMany(item => ((ObjItem)item).Quest)))
                                .Concat(this?.mapData.Scene.Npcs.SelectMany(item => item.Quest))
                                .Concat(this?.mapData.Scene.Front.Slots.SelectMany(item => ((BackItem)item).Quest))
                                .Concat(this?.mapData.Scene.Effect.Slots.Where(item => item is ParticleItem).SelectMany(item => ((ParticleItem)item).Quest))
                                .Concat(this?.mapData.Scene.Effect.Slots.Where(item => item is ParticleItem).SelectMany(item => ((ParticleItem)item).SubItems).SelectMany(item => item.Quest))
                                .Distinct().ToList();
                            this.ui.ChatBox.AppendTextHelp($"Number of related Quests: ({questList.Count()})");
                            foreach (QuestInfo item in questList)
                            {
                                Wz_Node questInfoNode = PluginBase.PluginManager.FindWz($@"Quest\QuestData\{item.ID}.img\QuestInfo")
                                    ?? PluginBase.PluginManager.FindWz($@"Quest\QuestInfo.img\{item.ID}");
                                string questName = questInfoNode?.Nodes["name"].GetValueEx<string>(null) ?? "null";
                                this.ui.ChatBox.AppendTextHelp($"  {questName}({item.ID}) / {item.State}");
                            }
                            break;

                        case "set":
                            if (Int32.TryParse(arguments.ElementAtOrDefault(2), out int questID) && questID > -1 && Int32.TryParse(arguments.ElementAtOrDefault(3), out int questState) && questState >= -1 && questState <= 2)
                            {
                                this.patchVisibility.SetQuestVisible(questID, questState);
                                this.mapData.PreloadResource(resLoader);
                                Wz_Node questInfoNode = PluginBase.PluginManager.FindWz($@"Quest\QuestData\{questID}.img\QuestInfo")
                                    ?? PluginBase.PluginManager.FindWz($@"Quest\QuestInfo.img\{questID}");
                                string questName = questInfoNode?.Nodes["name"].GetValueEx<string>(null) ?? "null";
                                this.ui.ChatBox.AppendTextSystem($"Changed the state of {questName}({questID}) to {questState}.");
                            }
                            else
                            {
                                this.ui.ChatBox.AppendTextSystem($"Please enter a correct Quest state.");
                            }
                            break;

                        default:
                            this.ui.ChatBox.AppendTextHelp(@"/quest list View list of related Quests");
                            this.ui.ChatBox.AppendTextHelp(@"/quest set (questID) (questState) Set the Quest state");
                            break;
                    }
                    break;

                case "/questex":
                    switch (arguments.ElementAtOrDefault(1))
                    {
                        case "list":
                            List<QuestExInfo> questList = this?.mapData.Scene.Layers.Nodes.SelectMany(l => ((LayerNode)l).Obj.Slots.SelectMany(item => ((ObjItem)item).Questex))
                                .Distinct().ToList();
                            this.ui.ChatBox.AppendTextHelp($"Number of related Quest keys:({questList.Count()})");
                            foreach (QuestExInfo item in questList)
                            {
                                Wz_Node questInfoNode = PluginBase.PluginManager.FindWz($@"Quest\QuestData\{item.ID}.img\QuestInfo")
                                    ?? PluginBase.PluginManager.FindWz($@"Quest\QuestInfo.img\{item.ID}");
                                string questName = questInfoNode?.Nodes["name"].GetValueEx<string>(null) ?? "null";
                                this.ui.ChatBox.AppendTextHelp($"{questName}({item.ID}) / Key: {item.Key}, Value: {item.State}");
                            }
                            break;

                        case "set":
                            string qkey = arguments.ElementAtOrDefault(3);
                            if (Int32.TryParse(arguments.ElementAtOrDefault(2), out int questID) && questID > -1 && Int32.TryParse(arguments.ElementAtOrDefault(4), out int questState) && questState >= -1 && qkey != null)
                            {
                                this.patchVisibility.SetQuestVisible(questID, qkey, questState);
                                this.mapData.PreloadResource(resLoader);
                                Wz_Node questInfoNode = PluginBase.PluginManager.FindWz($@"Quest\QuestData\{questID}.img\QuestInfo")
                                    ?? PluginBase.PluginManager.FindWz($@"Quest\QuestInfo.img\{questID}");
                                string questName = questInfoNode?.Nodes["name"].GetValueEx<string>(null) ?? "null";
                                this.ui.ChatBox.AppendTextSystem($"Changed the value of {questName}({questID}): key {qkey}) to {questState}.");
                            }
                            else
                            {
                                this.ui.ChatBox.AppendTextSystem($"Please enter the exact Quest ID, key, and value.");
                            }
                            break;

                        default:
                            this.ui.ChatBox.AppendTextHelp(@"/questex list View list of related Quest keys");
                            this.ui.ChatBox.AppendTextHelp(@"/questex set (questID) (key) (questState) Set the Quest key's value");
                            break;
                    }
                    break;

                default:
                    this.ui.ChatBox.AppendTextSystem($"Unknown command: {arguments[0]}");
                    break;
            }
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            this.ui.LoadContent(this.Content);
            this.renderEnv.Fonts.LoadContent(this.Content);
            this.isUnloaded = false;
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            opacity = MathHelper.Clamp(opacity, 0f, 1f);

            if (opacity <= 0)
            {
                this.GraphicsDevice.Clear(Color.Black);
            }
            else
            {
                if (prepareCapture)
                {
                    Capture(gameTime);
                }

                this.GraphicsDevice.Clear(Color.Black);
                if (this.mapData != null)
                {
                    DrawScene(gameTime);
                    DrawTooltipItems(gameTime);
                    DrawCaptureRect(gameTime);
                }
                this.ui.Draw(gameTime.ElapsedGameTime.TotalMilliseconds);
                this.tooltip.Draw(gameTime, renderEnv);
                if (opacity < 1f)
                {
                    this.renderEnv.Sprite.Begin(blendState: BlendState.NonPremultiplied);
                    var rect = new Rectangle(0, 0, this.renderEnv.Camera.Width, this.renderEnv.Camera.Height);
                    this.renderEnv.Sprite.FillRectangle(rect, new Color(Color.Black, 1 - opacity));
                    this.renderEnv.Sprite.End();
                }
            }

            base.Draw(gameTime);
        }

        #region 截图相关

        private bool CanCapture()
        {
            return (captureTask == null || captureTask.IsCompleted) && !prepareCapture;
        }

        private void Capture(GameTime gameTime)
        {
            if (this.mapData == null)
            {
                prepareCapture = false;
                return;
            }

            var oldTarget = GraphicsDevice.GetRenderTargets();

            //检查显卡支持纹理大小
            var maxTextureWidth = 4096;
            var maxTextureHeight = 4096;

            Rectangle originalWorldRect = this.renderEnv.Camera.WorldRect;
            Rectangle oldRect = originalWorldRect;
            // 보이는 화면만 캡쳐
            if (captureViewPortOnly)
            {
                oldRect = new Rectangle((int)this.renderEnv.Camera.Center.X - this.renderEnv.Camera.Width / 2, (int)this.renderEnv.Camera.Center.Y - this.renderEnv.Camera.Height / 2,
                    this.renderEnv.Camera.Width, this.renderEnv.Camera.Height);
            }
            // 스크린샷 커스텀 범위
            else if (!(this.CaptureRect.IsEmpty || this.CaptureRect.Width == 0 || this.CaptureRect.Height == 0))
            {
                oldRect = this.CaptureRect;
            }
            int width = Math.Min(oldRect.Width, maxTextureWidth);
            int height = Math.Min(oldRect.Height, maxTextureHeight);
            this.renderEnv.Camera.UseWorldRect = true;

            var target2d = new RenderTarget2D(this.GraphicsDevice, width, height, false, SurfaceFormat.Bgra32, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            PngEffect pngEffect = null;

            Color bgColor = Color.Black;
            var config = MapRenderConfig.Default;
            if (ColorWConverter.TryParse(config?.ScreenshotBackgroundColor?.Value, out var colorW))
            {
                bgColor = new Color(colorW.PackedValue);
            }
            Color bgColorPMA = bgColor.A switch
            {
                255 => bgColor,
                0 => Color.Transparent,
                _ => Color.FromNonPremultiplied(bgColor.ToVector4()),
            };

            //计算一组截图区
            int horizonBlock = (int)Math.Ceiling(1.0 * oldRect.Width / width);
            int verticalBlock = (int)Math.Ceiling(1.0 * oldRect.Height / height);
            byte[,][] picBlocks = new byte[horizonBlock, verticalBlock][];
            for (int j = 0; j < verticalBlock; j++)
            {
                for (int i = 0; i < horizonBlock; i++)
                {
                    //计算镜头区域
                    this.renderEnv.Camera.WorldRect = new Rectangle(
                        oldRect.X + i * width,
                        oldRect.Y + j * height,
                        width,
                        height);

                    //绘制
                    GraphicsDevice.SetRenderTarget(target2d);
                    GraphicsDevice.Clear(bgColorPMA);
                    DrawScene(gameTime);
                    //保存
                    if (bgColor.A == 255)
                    {
                        Texture2D t2d = target2d;
                        byte[] data = new byte[target2d.Width * target2d.Height * 4];
                        t2d.GetData(data);
                        picBlocks[i, j] = data;
                    }
                    else
                    {
                        if (pngEffect == null)
                        {
                            pngEffect = new PngEffect(this.GraphicsDevice);
                            pngEffect.AlphaMixEnabled = false;
                        }
                        using var texture = new RenderTarget2D(this.GraphicsDevice, width, height, false, SurfaceFormat.Bgra32, DepthFormat.None);
                        using var sb = new SpriteBatch(this.GraphicsDevice);
                        this.GraphicsDevice.SetRenderTarget(texture);
                        this.GraphicsDevice.Clear(Color.Transparent);
                        sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, null, null, pngEffect, null);
                        sb.Draw(target2d, Vector2.Zero, Color.White);
                        sb.End();
                        byte[] data = new byte[texture.Width * texture.Height * 4];
                        texture.GetData(data);
                        picBlocks[i, j] = data;
                    }
                }
            }
            pngEffect?.Dispose();
            target2d.Dispose();

            this.renderEnv.Camera.WorldRect = oldRect;
            this.renderEnv.Camera.UseWorldRect = false;

            GraphicsDevice.SetRenderTargets(oldTarget);
            prepareCapture = false;

            captureTask = Task.Factory.StartNew(() =>
                SaveTexture(picBlocks, oldRect.Width, oldRect.Height, width, height, bgColor.A == 255)
            );
        }

        private void SaveTexture(byte[,][] picBlocks, int mapWidth, int mapHeight, int blockWidth, int blockHeight, bool resetAlphaChannel)
        {
            //组装
            byte[] mapData = new byte[mapWidth * mapHeight * 4];
            for (int j = 0; j < picBlocks.GetLength(1); j++)
            {
                for (int i = 0; i < picBlocks.GetLength(0); i++)
                {
                    byte[] data = picBlocks[i, j];

                    Rectangle blockRect = new Rectangle();
                    blockRect.X = i * blockWidth;
                    blockRect.Y = j * blockHeight;
                    blockRect.Width = Math.Min(mapWidth - blockRect.X, blockWidth);
                    blockRect.Height = Math.Min(mapHeight - blockRect.Y, blockHeight);

                    int length = blockRect.Width * 4;
                    if (blockRect.X == 0 && blockRect.Width == mapWidth) //整块复制
                    {
                        int startIndex = mapWidth * 4 * blockRect.Y;
                        Buffer.BlockCopy(data, 0, mapData, startIndex, blockRect.Width * blockRect.Height * 4);
                    }
                    else //逐行扫描
                    {
                        int offset = 0;
                        for (int y = blockRect.Top, y0 = blockRect.Bottom; y < y0; y++)
                        {
                            int startIndex = (y * mapWidth + blockRect.X) * 4;
                            Buffer.BlockCopy(data, offset, mapData, startIndex, length);
                            offset += blockWidth * 4;
                        }
                    }
                }
            }

            if (resetAlphaChannel)
            {
                for (int i = 0; i < mapData.Length; i += 4)
                {
                    mapData[i + 3] = 255;
                }
            }

            try
            {
                using System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(
                    mapWidth,
                    mapHeight,
                    mapWidth * 4,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                    Marshal.UnsafeAddrOfPinnedArrayElement(mapData, 0));

                string outputFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + (this.mapData?.ID ?? 0).ToString("D9") + ".png";
                bitmap.Save(outputFileName, System.Drawing.Imaging.ImageFormat.Png);

                this.cm.StartCoroutine(cm.Post((v) =>
                {
                    v.ui.ChatBox.AppendTextHelp($"Screenshot Saved: {v.outputFileName}");
                }, new
                {
                    this.ui,
                    outputFileName
                }));
            }
            catch
            {
            }
        }
        #endregion

        #region 配置文件相关
        private void ApplySetting()
        {
            var config = MapRenderConfig.Default;
            Music.GlobalVolume = config.Volume;
            this.renderEnv.Camera.AdjustRectEnabled = config.ClipMapRegion;
            if (config.TopBarVisible)
            {
                this.ui.TopBar.Show();
            }
            else
            {
                this.ui.TopBar.Hide();
            }
            this.ui.Minimap.CameraRegionVisible = config.Minimap_CameraRegionVisible;
            this.ui.WorldMap.UseImageNameAsInfoName = config.WorldMap_UseImageNameAsInfoName;
            this.batcher.D2DEnabled = config.UseD2dRenderer;
            (this.Content as WcR2ContentManager).UseD2DFont = config.UseD2dRenderer;
            this.ForceCaptureWithResolution = config.ForceCaptureWithResolution;
        }

        private void LoadOptionData(UIOptionsDataModel model)
        {
            var config = MapRenderConfig.Default;
            model.SelectedFont = config.DefaultFontIndex;
            model.Volume = Music.GlobalVolume;
            model.MuteOnLeaveFocus = config.MuteOnLeaveFocus;
            model.ClipMapRegion = renderEnv.Camera.AdjustRectEnabled;
            model.UseD2dRenderer = config.UseD2dRenderer;
            model.NpcNameVisible = this.patchVisibility.NpcNameVisible;
            model.MobNameVisible = this.patchVisibility.MobNameVisible;
            model.TopBarVisible = this.ui.TopBar.Visibility == EmptyKeys.UserInterface.Visibility.Visible;
            model.ScreenshotBackgroundColor = config.ScreenshotBackgroundColor;
            model.Minimap_CameraRegionVisible = this.ui.Minimap.CameraRegionVisible;
            model.WorldMap_UseImageNameAsInfoName = this.ui.WorldMap.UseImageNameAsInfoName;
            model.ForceCaptureWithResolution = config.ForceCaptureWithResolution;
            LoadCaptureRectOptionData(model);
        }

        private void SaveOptionData(UIOptionsDataModel model)
        {
            WzComparerR2.Config.ConfigManager.Reload();
            var config = MapRenderConfig.Default;
            config.DefaultFontIndex = model.SelectedFont;
            config.Volume = model.Volume;
            config.MuteOnLeaveFocus = model.MuteOnLeaveFocus;
            config.ClipMapRegion = model.ClipMapRegion;
            config.UseD2dRenderer = model.UseD2dRenderer;
            this.patchVisibility.NpcNameVisible = model.NpcNameVisible;
            this.patchVisibility.MobNameVisible = model.MobNameVisible;
            config.TopBarVisible = model.TopBarVisible;
            config.ScreenshotBackgroundColor = model.ScreenshotBackgroundColor;
            config.Minimap_CameraRegionVisible = model.Minimap_CameraRegionVisible;
            config.WorldMap_UseImageNameAsInfoName = model.WorldMap_UseImageNameAsInfoName;
            config.ForceCaptureWithResolution = model.ForceCaptureWithResolution;
            WzComparerR2.Config.ConfigManager.Save();

            if (int.TryParse(model.ScLeft, out int left) && int.TryParse(model.ScTop, out int top)
                && int.TryParse(model.ScRight, out int right) && int.TryParse(model.ScBottom, out int bottom))
                this.CaptureRect = new Rectangle(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        }

        private void LoadCaptureRectOptionData(UIOptionsDataModel model)
        {
            if (this.CaptureRect.IsEmpty)
            {
                Rectangle src = this.renderEnv.Camera.WorldRect;

                model.ScLeft = src.Left.ToString();
                model.ScTop = src.Top.ToString();
                model.ScRight = src.Right.ToString();
                model.ScBottom = src.Bottom.ToString();
            }
            else
            {
                model.ScLeft = this.CaptureRect.Left.ToString();
                model.ScTop = this.CaptureRect.Top.ToString();
                model.ScRight = this.CaptureRect.Right.ToString();
                model.ScBottom = this.CaptureRect.Bottom.ToString();
            }
        }

        private void ResetCaptureRect()
        {
            // 해상도 기준으로 스크린샷 최소 크기 조절
            if (this.ForceCaptureWithResolution)
            {
                Rectangle src = this.renderEnv.Camera.WorldRect;
                if (src.Width < this.renderEnv.Camera.Width)
                {
                    src.X -= (this.renderEnv.Camera.Width - src.Width) / 2;
                    src.Width = this.renderEnv.Camera.Width;
                }
                if (src.Height < this.renderEnv.Camera.Height)
                {
                    src.Y -= (this.renderEnv.Camera.Height - src.Height) / 2;
                    src.Height = this.renderEnv.Camera.Height;
                }
                this.CaptureRect = src;
            }
            else this.CaptureRect = new Rectangle();
        }
        #endregion

        protected override void UnloadContent()
        {
            base.UnloadContent();

            if (!this.isUnloaded)
            {
                this.resLoader?.Unload();
                this.ui?.UnloadContents();
                this.Content.Unload();
                this.imeHelper?.Dispose();
                this.bgm = null;
                this.mapImg = null;
                this.mapData = null;
                this.isUnloaded = true;
            }
        }

        private void OnExiting()
        {
            if (this.isExiting)
            {
                return;
            }

            this.batcher?.Dispose();
            this.batcher = null;
            this.renderEnv?.Dispose();
            this.renderEnv = null;
            this.lightRenderer?.Dispose();
            this.lightRenderer = null;
            this.engine = null;

            foreach (var disposable in this.attachedEvent)
            {
                disposable.Dispose();
            }
            this.attachedEvent.Clear();
            this.ui?.InputBindings.Clear();

            GameExt.RemoveKeyboardEvent(this);
            GameExt.RemoveMouseStateCache();
            WcR2Engine.Unload();
            ServiceManager.Instance.RemoveService<IMEHandler>();
            this.isExiting = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.UnloadContent();
                this.OnExiting();
            }
            base.Dispose(disposing);
        }

        private void SwitchResolution()
        {
            var r = (Resolution)(((int)this.resolution + 1) % 9);
            SwitchResolution(r);
            ResetCaptureRect();
        }

        private void SwitchResolution(Resolution r)
        {
            Form gameWindow = (Form)Form.FromHandle(this.Window.Handle);
            int currentWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            int currentHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            switch (r)
            {
                case Resolution.Window_800_600:
                    gameWindow.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
                    break;
                case Resolution.Window_1024_768:
                case Resolution.Window_1280_720:
                case Resolution.Window_1366_768:
                case Resolution.Window_1920_1080:
                case Resolution.Window_2560_1080:
                case Resolution.Window_2560_1440:
                case Resolution.Window_3440_1440:
                    string[] resolutionName = r.ToString().Split('_');
                    if (currentWidth <= Int32.Parse(resolutionName[1]) || currentHeight <= Int32.Parse(resolutionName[2]))
                    {
                        r = Resolution.WindowFullScreen;
                        gameWindow.SetDesktopLocation(0, 0);
                        gameWindow.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    }
                    else
                    {
                        gameWindow.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
                    }
                    break;
                case Resolution.WindowFullScreen:
                    gameWindow.SetDesktopLocation(0, 0);
                    gameWindow.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    break;
                default:
                    r = Resolution.Window_800_600;
                    goto case Resolution.Window_800_600;
            }

            this.resolution = r;
            this.renderEnv.Camera.DisplayMode = (int)r;
            this.ui.Width = this.renderEnv.Camera.Width;
            this.ui.Height = this.renderEnv.Camera.Height;
            engine.Renderer.ResetNativeSize();
        }

        private IDisposable EventDisposable<TDelegate>(TDelegate arg, Action<TDelegate> action)
        {
            return new Disposable<TDelegate>(action, arg);
        }

        enum Resolution
        {
            Window_800_600 = 0,
            Window_1024_768 = 1,
            Window_1280_720 = 2,
            Window_1366_768 = 3,
            Window_1920_1080 = 4,
            Window_2560_1080 = 5,
            Window_2560_1440 = 6,
            Window_3440_1440 = 7,
            WindowFullScreen = 8,
        }

        struct ItemRect
        {
            public SceneItem item;
            public Rectangle rect;
        }

        class Disposable<TDelegate> : IDisposable
        {
            public Disposable(Action<TDelegate> action, TDelegate arg)
            {
                this.Action = action;
                this.Arg = arg;
            }

            public readonly Action<TDelegate> Action;
            public readonly TDelegate Arg;

            public void Dispose()
            {
                this.Action?.Invoke(this.Arg);
            }
        }
    }
}
