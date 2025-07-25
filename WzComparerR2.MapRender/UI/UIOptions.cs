﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Media;
using EmptyKeys.UserInterface.Data;
using EmptyKeys.UserInterface.Renderers;
using EmptyKeys.UserInterface.Media.Imaging;
using EmptyKeys.UserInterface.Mvvm;

namespace WzComparerR2.MapRender.UI
{
    class UIOptions : WindowEx
    {
        public UIOptions()
        {

        }

        public event EventHandler OK;
        public event EventHandler Cancel;
        public event EventHandler ResetSCRect;
        public event EventHandler ChkForceClickEvent;

        protected override void InitializeComponents()
        {
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(30) });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(16) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(30) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.SetBinding(BackgroundProperty, new Binding(Control.BackgroundProperty) { Source = this });
            this.Content = grid;

            TextBlock title = new TextBlock();
            title.Text = "Settings";
            title.IsHitTestVisible = false;
            title.Foreground = Brushes.Gold;
            title.HorizontalAlignment = HorizontalAlignment.Center;
            title.VerticalAlignment = VerticalAlignment.Center;

            Border header = new Border();
            header.Child = title;
            grid.Children.Add(header);
            Grid.SetRow(header, 0);
            Grid.SetColumn(header, 0);
            this.SetDragTarget(header);

            TabItem tab1 = new TabItem();
            tab1.Header = "General";
            tab1.Content = GetTabContent1();

            TabItem tab2 = new TabItem();
            tab2.Header = "Status Bar";
            tab2.Content = GetTabContent2();

            TabItem tab3 = new TabItem();
            tab3.Header = "Minimap";
            tab3.Content = GetTabContent3();

            TabItem tab4 = new TabItem();
            tab4.Header = "World Map";
            tab4.Content = GetTabContent4();

            TabItem tab5 = new TabItem();
            tab5.Header = "Screenshot Range";
            tab5.Content = GetTabContent5();

            TabItem tab6 = new TabItem();
            tab6.Header = "Help";
            tab6.Content = GetTabContent6();

            TabControl tabControl = new TabControl();
            tabControl.Resources.Add(typeof(TabItem), GetTabItemStyle());
            tabControl.Margin = new Thickness(5, 0, 5, 0);
            tabControl.TabStripPlacement = Dock.Left;
            tabControl.ItemsSource = new[] { tab1, tab2, tab3, tab4, tab5, tab6 };
            grid.Children.Add(tabControl);
            Grid.SetRow(tabControl, 1);
            Grid.SetColumn(tabControl, 0);

            TextBlock lblHint = new TextBlock();
            lblHint.Foreground = Brushes.Yellow;
            lblHint.VerticalAlignment = VerticalAlignment.Center;
            lblHint.Text = "* Some settings require a restart of MapRender.";
            lblHint.Margin = new Thickness(20, 0, 0, 0);
            grid.Children.Add(lblHint);
            Grid.SetRow(lblHint, 2);
            Grid.SetColumn(lblHint, 0);

            Button btnOK = new Button();
            btnOK.Width = 50;
            btnOK.Height = 20;
            btnOK.Margin = new Thickness(5);
            btnOK.Content = "Confirm";
            btnOK.Click += BtnOK_Click;

            Button btnCancel = new Button();
            btnCancel.Width = 50;
            btnCancel.Height = 20;
            btnCancel.Margin = new Thickness(5);
            btnCancel.Content = "Cancel";
            btnCancel.Click += BtnCancel_Click;

            StackPanel footerPanel = new StackPanel();
            footerPanel.HorizontalAlignment = HorizontalAlignment.Center;
            footerPanel.VerticalAlignment = VerticalAlignment.Center;
            footerPanel.Orientation = Orientation.Horizontal;
            footerPanel.Children.Add(btnOK);
            footerPanel.Children.Add(btnCancel);

            Border footer = new Border();
            footer.Child = footerPanel;
            grid.Children.Add(footer);
            Grid.SetRow(footer, 3);
            Grid.SetColumn(footer, 0);

            this.Width = 450;
            this.Height = 300;
            this.SetResourceReference(BackgroundProperty, MapRenderResourceKey.TooltipBrush);
            base.InitializeComponents();
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            this.OK?.Invoke(this, EventArgs.Empty);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Cancel?.Invoke(this, EventArgs.Empty);
        }

        private void BtnSCReset_Click(object sender, RoutedEventArgs e)
        {
            this.ResetSCRect?.Invoke(this, EventArgs.Empty);
            return;
        }

        private void ChkForce_Click(object sender, RoutedEventArgs e)
        {
            this.ChkForceClickEvent?.Invoke(this, EventArgs.Empty);
            return;
        }

        private UIElement GetTabContent1()
        {
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            //grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            //grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            TextBlock lbl1 = new TextBlock();
            lbl1.VerticalAlignment = VerticalAlignment.Center;
            lbl1.Text = "BGM";
            lbl1.Foreground = Brushes.Yellow;
            Grid.SetRow(lbl1, 0);
            Grid.SetColumn(lbl1, 0);
            grid.Children.Add(lbl1);

            CheckBox chk1 = new CheckBox();
            chk1.Content = "Mute when in background";
            chk1.Margin = new Thickness(18, 0, 0, 0);
            chk1.Background = Brushes.Gray;
            chk1.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(UIOptionsDataModel.MuteOnLeaveFocus)));
            Grid.SetRow(chk1, 1);
            Grid.SetColumn(chk1, 0);
            Grid.SetColumnSpan(chk1, 2);
            grid.Children.Add(chk1);

            StackPanel pnl1 = new StackPanel();
            pnl1.Orientation = Orientation.Horizontal;
            Grid.SetRow(pnl1, 2);
            Grid.SetColumn(pnl1, 0);
            Grid.SetColumnSpan(pnl1, 2);
            grid.Children.Add(pnl1);

            TextBlock lbl2 = new TextBlock();
            lbl2.TextAlignment = TextAlignment.Center;
            lbl2.HorizontalAlignment = HorizontalAlignment.Center;
            lbl2.VerticalAlignment = VerticalAlignment.Center;
            lbl2.Padding = new Thickness(24, 0, 0, 0);
            lbl2.Text = "Volume";
            pnl1.Children.Add(lbl2);

            Slider slider1 = new Slider();
            slider1.Minimum = 0f;
            slider1.Maximum = 1f;
            slider1.Width = 100;
            slider1.SetBinding(Slider.ValueProperty, new Binding(nameof(UIOptionsDataModel.Volume)));
            pnl1.Children.Add(slider1);

            TextBlock lblVol = new TextBlock();
            lblVol.TextAlignment = TextAlignment.Center;
            lblVol.HorizontalAlignment = HorizontalAlignment.Left;
            lblVol.VerticalAlignment = VerticalAlignment.Center;
            lblVol.Padding = new Thickness(12, 0, 0, 0);
            lblVol.SetBinding(TextBlock.TextProperty, new Binding(Slider.ValueProperty)
            {
                Source = slider1,
                Converter = UIHelper.CreateConverter(o => string.Format("{0:0}", ((float)o * 100)))
            });
            pnl1.Children.Add(lblVol);

            TextBlock lbl3 = new TextBlock();
            lbl3.VerticalAlignment = VerticalAlignment.Center;
            lbl3.Text = "Default Font";
            lbl3.Foreground = Brushes.Yellow;
            Grid.SetRow(lbl3, 3);
            Grid.SetColumn(lbl3, 0);
            grid.Children.Add(lbl3);

            ComboBox cmb1 = new ComboBox();
            cmb1.ItemsSource = (IEnumerable<string>)this.FindResource(MapRenderResourceKey.FontList); //source reference has bugs.
            cmb1.SetBinding(ComboBox.SelectedIndexProperty, new Binding(nameof(UIOptionsDataModel.SelectedFont)));
            Grid.SetRow(cmb1, 3);
            Grid.SetColumn(cmb1, 1);
            grid.Children.Add(cmb1);

            TextBlock lbl4 = new TextBlock();
            lbl4.VerticalAlignment = VerticalAlignment.Center;
            lbl4.Text = "Map Window";
            lbl4.Foreground = Brushes.Yellow;
            Grid.SetRow(lbl4, 4);
            Grid.SetColumn(lbl4, 0);
            grid.Children.Add(lbl4);

            CheckBox chk2 = new CheckBox();
            chk2.Content = "Map Range Limit";
            chk2.Margin = new Thickness(18, 0, 0, 0);
            chk2.Background = Brushes.Gray;
            chk2.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(UIOptionsDataModel.ClipMapRegion)));
            Grid.SetRow(chk2, 5);
            Grid.SetColumn(chk2, 0);
            Grid.SetColumnSpan(chk2, 2);
            grid.Children.Add(chk2);

            TextBlock lbl5 = new TextBlock();
            lbl5.VerticalAlignment = VerticalAlignment.Center;
            lbl5.Text = "Rendering";
            lbl5.Foreground = Brushes.Yellow;
            Grid.SetRow(lbl5, 6);
            Grid.SetColumn(lbl5, 0);
            grid.Children.Add(lbl5);

            CheckBox chk3 = new CheckBox();
            chk3.Content = "Use D2D Renderer";
            chk3.Margin = new Thickness(18, 0, 0, 0);
            chk3.Background = Brushes.Gray;
            chk3.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(UIOptionsDataModel.UseD2dRenderer)));
            Grid.SetRow(chk3, 7);
            Grid.SetColumn(chk3, 0);
            Grid.SetColumnSpan(chk3, 2);
            grid.Children.Add(chk3);

            CheckBox chk4 = new CheckBox();
            chk4.Content = "Show NPC Name";
            chk4.Margin = new Thickness(18, 0, 0, 0);
            chk4.Background = Brushes.Gray;
            chk4.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(UIOptionsDataModel.NpcNameVisible)));
            Grid.SetRow(chk4, 8);
            Grid.SetColumn(chk4, 0);
            Grid.SetColumnSpan(chk4, 2);
            grid.Children.Add(chk4);

            CheckBox chk5 = new CheckBox();
            chk5.Content = "Show Mob Name";
            chk5.Margin = new Thickness(18, 0, 0, 0);
            chk5.Background = Brushes.Gray;
            chk5.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(UIOptionsDataModel.MobNameVisible)));
            Grid.SetRow(chk5, 9);
            Grid.SetColumn(chk5, 0);
            Grid.SetColumnSpan(chk5, 2);
            grid.Children.Add(chk5);

            /*TextBlock lbl6 = new TextBlock();
            lbl6.VerticalAlignment = VerticalAlignment.Center;
            lbl6.Text = "Screenshot";
            lbl6.Foreground = Brushes.Yellow;
            Grid.SetRow(lbl6, 10);
            Grid.SetColumn(lbl6, 0);
            grid.Children.Add(lbl6);

            StackPanel pnl2 = new StackPanel();
            pnl2.Orientation = Orientation.Horizontal;
            Grid.SetRow(pnl2, 11);
            Grid.SetColumn(pnl2, 0);
            Grid.SetColumnSpan(pnl2, 2);
            grid.Children.Add(pnl2);

            TextBlock lbl7 = new TextBlock();
            lbl7.TextAlignment = TextAlignment.Center;
            lbl7.HorizontalAlignment = HorizontalAlignment.Center;
            lbl7.VerticalAlignment = VerticalAlignment.Center;
            lbl7.Padding = new Thickness(24, 0, 0, 0);
            lbl7.Text = "Background (ARGB)";
            pnl2.Children.Add(lbl7);

            TextBox tb1 = new TextBox();
            tb1.Width = 60;
            tb1.HorizontalAlignment = HorizontalAlignment.Center;
            tb1.VerticalAlignment = VerticalAlignment.Center;
            tb1.MaxLength = 8;
            tb1.SetBinding(TextBox.TextProperty, new Binding(nameof(UIOptionsDataModel.ScreenshotBackgroundColor)));
            pnl2.Children.Add(tb1);

            Canvas img1 = new Canvas();
            img1.Width = 20;
            img1.Height = 20;
            img1.Margin = new Thickness(2);
            img1.SetBinding(Canvas.BackgroundProperty, new Binding(nameof(UIOptionsDataModel.ScreenshotBackgroundColor))
            {
                Converter = UIHelper.CreateConverter((string s) => ColorWConverter.TryParse(s, out var color) ? new SolidColorBrush(color) : null)
            });
            pnl2.Children.Add(img1);*/

            ScrollViewer viewer = new ScrollViewer();
            viewer.Content = grid;
            return viewer;
        }

        private UIElement GetTabContent2()
        {
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            CheckBox chk1 = new CheckBox();
            chk1.Content = "Open Status Bar";
            chk1.Background = Brushes.Gray;
            chk1.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(UIOptionsDataModel.TopBarVisible)));
            Grid.SetRow(chk1, 0);
            Grid.SetColumn(chk1, 0);
            Grid.SetColumnSpan(chk1, 2);
            grid.Children.Add(chk1);

            return grid;
        }

        private UIElement GetTabContent3()
        {
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            CheckBox chk1 = new CheckBox();
            chk1.Content = "Display Visible Areas";
            chk1.Background = Brushes.Gray;
            chk1.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(UIOptionsDataModel.Minimap_CameraRegionVisible)));
            Grid.SetRow(chk1, 0);
            Grid.SetColumn(chk1, 0);
            Grid.SetColumnSpan(chk1, 2);
            grid.Children.Add(chk1);

            return grid;
        }

        private UIElement GetTabContent4()
        {
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            CheckBox chk1 = new CheckBox();
            chk1.Content = "Use ImageName As InfoName";
            chk1.Background = Brushes.Gray;
            chk1.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(UIOptionsDataModel.WorldMap_UseImageNameAsInfoName)));
            Grid.SetRow(chk1, 0);
            Grid.SetColumn(chk1, 0);
            Grid.SetColumnSpan(chk1, 2);
            grid.Children.Add(chk1);

            return grid;
        }

        private UIElement GetTabContent5()
        {
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(24, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(60, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(60, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(60, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(60, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());

            TextBlock lblSCRect = new TextBlock();
            lblSCRect.VerticalAlignment = VerticalAlignment.Center;
            lblSCRect.Text = "Range";
            lblSCRect.Foreground = Brushes.Yellow;
            Grid.SetRow(lblSCRect, 0);
            Grid.SetColumn(lblSCRect, 0);
            grid.Children.Add(lblSCRect);

            TextBlock lblSCLeft = new TextBlock();
            lblSCLeft.HorizontalAlignment = HorizontalAlignment.Center;
            lblSCLeft.VerticalAlignment = VerticalAlignment.Center;
            lblSCLeft.Text = "Left";
            Grid.SetRow(lblSCLeft, 3);
            Grid.SetColumn(lblSCLeft, 0);
            grid.Children.Add(lblSCLeft);

            TextBlock lblSCTop = new TextBlock();
            lblSCTop.HorizontalAlignment = HorizontalAlignment.Center;
            lblSCTop.VerticalAlignment = VerticalAlignment.Center;
            lblSCTop.Text = "Top";
            Grid.SetRow(lblSCTop, 1);
            Grid.SetColumn(lblSCTop, 1);
            grid.Children.Add(lblSCTop);

            TextBlock lblSCRight = new TextBlock();
            lblSCRight.HorizontalAlignment = HorizontalAlignment.Center;
            lblSCRight.VerticalAlignment = VerticalAlignment.Center;
            lblSCRight.Text = "Right";
            Grid.SetRow(lblSCRight, 3);
            Grid.SetColumn(lblSCRight, 2);
            grid.Children.Add(lblSCRight);

            TextBlock lblSCBottom = new TextBlock();
            lblSCBottom.HorizontalAlignment = HorizontalAlignment.Center;
            lblSCBottom.VerticalAlignment = VerticalAlignment.Center;
            lblSCBottom.Text = "Bottom";
            Grid.SetRow(lblSCBottom, 5);
            Grid.SetColumn(lblSCBottom, 1);
            grid.Children.Add(lblSCBottom);

            TextBox tbSCLeft = new TextBox();
            tbSCLeft.Width = 60;
            tbSCLeft.HorizontalAlignment = HorizontalAlignment.Center;
            tbSCLeft.VerticalAlignment = VerticalAlignment.Center;
            tbSCLeft.MaxLength = 6;
            tbSCLeft.SetBinding(TextBox.TextProperty, new Binding(nameof(UIOptionsDataModel.ScLeft)));
            Grid.SetRow(tbSCLeft, 4);
            Grid.SetColumn(tbSCLeft, 0);
            grid.Children.Add(tbSCLeft);

            TextBox tbSCTop = new TextBox();
            tbSCTop.Width = 60;
            tbSCTop.HorizontalAlignment = HorizontalAlignment.Center;
            tbSCTop.VerticalAlignment = VerticalAlignment.Center;
            tbSCTop.MaxLength = 6;
            tbSCTop.SetBinding(TextBox.TextProperty, new Binding(nameof(UIOptionsDataModel.ScTop)));
            Grid.SetRow(tbSCTop, 2);
            Grid.SetColumn(tbSCTop, 1);
            grid.Children.Add(tbSCTop);

            TextBox tbSCRight = new TextBox();
            tbSCRight.Width = 60;
            tbSCRight.HorizontalAlignment = HorizontalAlignment.Center;
            tbSCRight.VerticalAlignment = VerticalAlignment.Center;
            tbSCRight.MaxLength = 6;
            tbSCRight.SetBinding(TextBox.TextProperty, new Binding(nameof(UIOptionsDataModel.ScRight)));
            Grid.SetRow(tbSCRight, 4);
            Grid.SetColumn(tbSCRight, 2);

            TextBox tbSCBottom = new TextBox();
            tbSCBottom.Width = 60;
            tbSCBottom.HorizontalAlignment = HorizontalAlignment.Center;
            tbSCBottom.VerticalAlignment = VerticalAlignment.Center;
            tbSCBottom.MaxLength = 6;
            tbSCBottom.SetBinding(TextBox.TextProperty, new Binding(nameof(UIOptionsDataModel.ScBottom)));
            Grid.SetRow(tbSCBottom, 6);
            Grid.SetColumn(tbSCBottom, 1);
            grid.Children.Add(tbSCBottom);
            grid.Children.Add(tbSCRight);

            Button btnSCReset = new Button();
            btnSCReset.Width = 60;
            btnSCReset.Height = 20;
            btnSCReset.Margin = new Thickness(5);
            btnSCReset.Content = "Reset";
            btnSCReset.Click += BtnSCReset_Click;
            Grid.SetRow(btnSCReset, 6);
            Grid.SetColumn(btnSCReset, 3);
            grid.Children.Add(btnSCReset);

            CheckBox chkForce = new CheckBox();
            chkForce.Content = "Set minimum size to current resolution";
            chkForce.Background = Brushes.Gray;
            chkForce.Click += ChkForce_Click;
            chkForce.SetBinding(CheckBox.IsCheckedProperty, new Binding(nameof(UIOptionsDataModel.ForceCaptureWithResolution)));
            Grid.SetRow(chkForce, 7);
            Grid.SetColumn(chkForce, 0);
            Grid.SetColumnSpan(chkForce, 4);
            grid.Children.Add(chkForce);

            /***************/

            TextBlock lbl6 = new TextBlock();
            lbl6.VerticalAlignment = VerticalAlignment.Center;
            lbl6.Text = "Background Color (ARGB)";
            lbl6.Foreground = Brushes.Yellow;
            Grid.SetRow(lbl6, 8);
            Grid.SetColumn(lbl6, 0);
            Grid.SetColumnSpan(lbl6, 4);
            grid.Children.Add(lbl6);

            StackPanel pnl2 = new StackPanel();
            pnl2.Orientation = Orientation.Horizontal;
            Grid.SetRow(pnl2, 9);
            Grid.SetColumn(pnl2, 0);
            Grid.SetColumnSpan(pnl2, 4);
            grid.Children.Add(pnl2);

            TextBox tb1 = new TextBox();
            tb1.Width = 60;
            tb1.HorizontalAlignment = HorizontalAlignment.Center;
            tb1.VerticalAlignment = VerticalAlignment.Center;
            tb1.MaxLength = 8;
            tb1.SetBinding(TextBox.TextProperty, new Binding(nameof(UIOptionsDataModel.ScreenshotBackgroundColor)));
            pnl2.Children.Add(tb1);

            Canvas img1 = new Canvas();
            img1.Width = 20;
            img1.Height = 20;
            img1.Margin = new Thickness(2);
            img1.SetBinding(Canvas.BackgroundProperty, new Binding(nameof(UIOptionsDataModel.ScreenshotBackgroundColor))
            {
                Converter = UIHelper.CreateConverter((string s) => ColorWConverter.TryParse(s, out var color) ? new SolidColorBrush(color) : null)
            });
            pnl2.Children.Add(img1);

            /**************/

            ScrollViewer viewer = new ScrollViewer();
            viewer.Content = grid;
            return viewer;
        }

        private UIElement GetTabContent6()
        {
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Vertical;
            
            var tips = new[]
            {
                 "Hotkeys:",
                 "",
                 "[M] Minimap",
                 "[W] World Map",
                 "[Esc] Setup",
                 "[Ctrl+1~0] Switch Layer",
                 "[Ctrl+U] Remove Map Range Limit",
                 "[`] Chat Window",
                 "[Alt+Enter] Change Resolution",
                 "[S] Show Screenshot Range",
                 "[ScrollLock] Screenshot",
            };

            foreach (var tip in tips)
            {
                TextBlock lbl = new TextBlock();
                lbl.TextWrapping = TextWrapping.Wrap;
                lbl.Text = tip;
                lbl.Margin = new Thickness(0, 1, 0, 1);
                panel.Children.Add(lbl);
            }

            ScrollViewer viewer = new ScrollViewer();
            viewer.Content = panel;
            return viewer;
        }

        private Style GetTabItemStyle()
        {
            var style = EmptyKeys.UserInterface.Themes.TabControlStyle.CreateTabItemStyle();
            var templateSetter = style.Setters.FirstOrDefault(s => s.Property == Control.TemplateProperty);
            if (templateSetter != null)
            {
                var oldTemplate = templateSetter.Value as ControlTemplate;
                var funcType = typeof(Func<UIElement, UIElement>);
                var funcField = oldTemplate.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(field => field.FieldType == funcType);
                var oldMethod = funcField?.GetValue(oldTemplate) as Func<UIElement, UIElement>;
                if (oldMethod != null)
                {
                    var newMethod = new Func<UIElement, UIElement>(parent =>
                    {
                        UIElement elem = oldMethod(parent);
                        ContentPresenter presenter = (elem as Border)?.Child as ContentPresenter;
                        if (presenter != null)
                        {
                            presenter.Margin = new Thickness(6, 1, 6, 1);
                        }
                        return elem;
                    });
                    funcField.SetValue(oldTemplate, newMethod);
                }
            }

            return style;
        }
    }

    class UIOptionsDataModel : BindableBase
    {
        private bool _muteOnLeaveFocus;
        private float _volume;
        private int _selectedFont;
        private bool _clipMapRegion;
        private bool _useD2dRenderer;
        private bool _npcNameVisible;
        private bool _mobNameVisible;
        private bool _topBarVisible;
        private bool _minimap_cameraRegionVisible;
        private bool _worldmap_useImageNameAsInfoName;
        private bool _forceCaptureWithResolution;
        private string _screenshotBackgroundColor;
        private string _scLeft;
        private string _scTop;
        private string _scRight;
        private string _scBottom;

        public bool MuteOnLeaveFocus
        {
            get { return this._muteOnLeaveFocus; }
            set { base.SetProperty(ref this._muteOnLeaveFocus, value); }
        }

        public float Volume
        {
            get { return this._volume; }
            set { base.SetProperty(ref this._volume, value); }
        }

        public int SelectedFont
        {
            get { return this._selectedFont; }
            set { base.SetProperty(ref this._selectedFont, value); }
        }

        public bool ClipMapRegion
        {
            get { return this._clipMapRegion; }
            set { base.SetProperty(ref this._clipMapRegion, value); }
        }

        public bool UseD2dRenderer
        {
            get { return this._useD2dRenderer; }
            set { base.SetProperty(ref this._useD2dRenderer, value); }
        }

        public bool NpcNameVisible
        {
            get { return this._npcNameVisible; }
            set { base.SetProperty(ref this._npcNameVisible, value); }
        }

        public bool MobNameVisible
        {
            get { return this._mobNameVisible; }
            set { base.SetProperty(ref this._mobNameVisible, value); }
        }

        public string ScreenshotBackgroundColor
        {
            get { return this._screenshotBackgroundColor; }
            set { base.SetProperty(ref this._screenshotBackgroundColor, value); }
        }

        public bool TopBarVisible
        {
            get { return this._topBarVisible; }
            set { base.SetProperty(ref this._topBarVisible, value); }
        }

        public bool Minimap_CameraRegionVisible
        {
            get { return this._minimap_cameraRegionVisible; }
            set { base.SetProperty(ref this._minimap_cameraRegionVisible, value); }
        }

        public bool WorldMap_UseImageNameAsInfoName
        {
            get { return this._worldmap_useImageNameAsInfoName; }
            set { base.SetProperty(ref this._worldmap_useImageNameAsInfoName, value); }
        }

        public bool ForceCaptureWithResolution
        {
            get { return this._forceCaptureWithResolution; }
            set { base.SetProperty(ref this._forceCaptureWithResolution, value); }
        }

        public string ScLeft
        {
            get { return this._scLeft; }
            set { base.SetProperty(ref this._scLeft, value); }
        }

        public string ScTop
        {
            get { return this._scTop; }
            set { base.SetProperty(ref this._scTop, value); }
        }

        public string ScBottom
        {
            get { return this._scBottom; }
            set { base.SetProperty(ref this._scBottom, value); }
        }

        public string ScRight
        {
            get { return this._scRight; }
            set { base.SetProperty(ref this._scRight, value); }
        }
    }
}