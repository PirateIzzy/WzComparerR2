using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Data;
using EmptyKeys.UserInterface.Themes;
using EmptyKeys.UserInterface.Media.Imaging;
using MRes = WzComparerR2.MapRender.Properties.Resources;

namespace WzComparerR2.MapRender.UI
{
    static class MapRenderButtonStyle
    {
        public static Style CreateMapRenderButtonStyle()
        {
            var style = ImageButtonStyle.CreateImageButtonStyle();

            //btnOK
            var trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "OK"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("OK"));
            style.Triggers.Add(trigger);
            //btnYes
            trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "Yes"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("Yes"));
            style.Triggers.Add(trigger);
            //btnNo
            trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "No"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("No"));
            style.Triggers.Add(trigger);
            //btnCancel
            trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "Cancel"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("Cancel"));
            style.Triggers.Add(trigger);
            //btnGoToCurrentMap
            trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "GoToCurrentMap"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("GoToCurrentMap"));
            style.Triggers.Add(trigger);
            //btnReturnToTown
            trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "ReturnToTown"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("ReturnToTown"));
            style.Triggers.Add(trigger);
            //btnShowWorldMap
            trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "ShowWorldMap"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("ShowWorldMap"));
            style.Triggers.Add(trigger);
            //btnNavigation
            trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "Navigation"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("Navigation"));
            style.Triggers.Add(trigger);
            //btnNpcList
            trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "NpcList"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("NpcList"));
            style.Triggers.Add(trigger);
            //btnFilter
            trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "Filter"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("Filter"));
            style.Triggers.Add(trigger);
            //btnBack
            trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "Back"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("Back"));
            style.Triggers.Add(trigger);
            //btnClose
            trigger = new Trigger()
            {
                Property = UIElement.NameProperty,
                Value = "Close"
            };
            trigger.Setters.AddRange(GetMapRenderButtonSetters("Close"));
            style.Triggers.Add(trigger);

            return style;
        }

        public static Setter[] GetMapRenderButtonSetters(string name)
        {
            switch (name)
            {
                case "OK":
                case "Yes":
                    return CreateButtonSetters(42, 16,
                        nameof(MRes.Basic_img_BtOK4_normal_0),
                        nameof(MRes.Basic_img_BtOK4_mouseOver_0),
                        nameof(MRes.Basic_img_BtOK4_pressed_0),
                        nameof(MRes.Basic_img_BtOK4_disabled_0));

                case "No":
                    return CreateButtonSetters(42, 16,
                        nameof(MRes.Basic_img_BtNo3_normal_0),
                        nameof(MRes.Basic_img_BtNo3_mouseOver_0),
                        nameof(MRes.Basic_img_BtNo3_pressed_0),
                        nameof(MRes.Basic_img_BtNo3_disabled_0));

                case "Cancel":
                    return CreateButtonSetters(42, 16,
                        nameof(MRes.Basic_img_BtCancel4_normal_0),
                        nameof(MRes.Basic_img_BtCancel4_mouseOver_0),
                        nameof(MRes.Basic_img_BtCancel4_pressed_0),
                        nameof(MRes.Basic_img_BtCancel4_disabled_0));

                case "GoToCurrentMap":
                    return CreateButtonSetters(37, 25,
                        nameof(MRes.UI_UIMap_img_WorldMap_button_goToCurrentMap_normal_0),
                        nameof(MRes.UI_UIMap_img_WorldMap_button_goToCurrentMap_mouseOver_0),
                        nameof(MRes.UI_UIMap_img_WorldMap_button_goToCurrentMap_pressed_0),
                        nameof(MRes.UI_UIMap_img_WorldMap_button_goToCurrentMap_disabled_0));

                case "Back":
                    return CreateButtonSetters(37, 25,
                        nameof(MRes.UI_UIMap_img_WorldMap_button_goToUpperWorldMap_normal_0),
                        nameof(MRes.UI_UIMap_img_WorldMap_button_goToUpperWorldMap_mouseOver_0),
                        nameof(MRes.UI_UIMap_img_WorldMap_button_goToUpperWorldMap_pressed_0),
                        nameof(MRes.UI_UIMap_img_WorldMap_button_goToUpperWorldMap_disabled_0));

                case "ReturnToTown":
                    return CreateButtonSetters(21, 21,
                        nameof(MRes.UI_UIMap_img_MiniMap_BtTown_normal_0),
                        nameof(MRes.UI_UIMap_img_MiniMap_BtTown_mouseOver_0),
                        nameof(MRes.UI_UIMap_img_MiniMap_BtTown_pressed_0),
                        nameof(MRes.UI_UIMap_img_MiniMap_BtTown_disabled_0));

                case "ShowWorldMap":
                    return CreateButtonSetters(21, 21,
                        nameof(MRes.UI_UIMap_img_MiniMap_BtMap_normal_0),
                        nameof(MRes.UI_UIMap_img_MiniMap_BtMap_mouseOver_0),
                        nameof(MRes.UI_UIMap_img_MiniMap_BtMap_pressed_0),
                        nameof(MRes.UI_UIMap_img_MiniMap_BtMap_disabled_0));

                case "Navigation":
                    return CreateButtonSetters(21, 21,
                        nameof(MRes.UI_UIMap_img_MiniMap_BtNavigation_normal_0),
                        nameof(MRes.UI_UIMap_img_MiniMap_BtNavigation_mouseOver_0),
                        nameof(MRes.UI_UIMap_img_MiniMap_BtNavigation_pressed_0),
                        nameof(MRes.UI_UIMap_img_MiniMap_BtNavigation_disabled_0));

                case "NpcList":
                    return CreateButtonSetters(21, 21,
                        nameof(MRes.UI_UIMap_img_MiniMap_BtNpc_normal_0),
                        nameof(MRes.UI_UIMap_img_MiniMap_BtNpc_mouseOver_0),
                        nameof(MRes.UI_UIMap_img_MiniMap_BtNpc_pressed_0),
                        nameof(MRes.UI_UIMap_img_MiniMap_BtNpc_disabled_0));

                case "Filter":
                    return CreateButtonSetters(21, 21,
                        nameof(MRes.UI_UIMap_img_MiniMap_BtFilter_normal_0),
                        nameof(MRes.UI_UIMap_img_MiniMap_BtFilter_mouseOver_0),
                        nameof(MRes.UI_UIMap_img_MiniMap_BtFilter_pressed_0),
                        nameof(MRes.UI_UIMap_img_MiniMap_BtFilter_disabled_0));

                case "Close":
                    return CreateButtonSetters(11, 11,
                         nameof(MRes.UI_UIMap_img_WorldMap_button_close_normal_0),
                         nameof(MRes.UI_UIMap_img_WorldMap_button_close_mouseOver_0),
                         nameof(MRes.UI_UIMap_img_WorldMap_button_close_pressed_0),
                         nameof(MRes.UI_UIMap_img_WorldMap_button_close_disabled_0));

                default:
                    return null;
            }
        }

        private static Setter[] CreateButtonSetters(float width, float height,
            string normalAsset, string mouseOverAsset, string pressedAsset, string disabledAsset)
        {
            return new Setter[]
            {
                new Setter(UIElement.WidthProperty, width),
                new Setter(UIElement.HeightProperty, height),
                new Setter(ImageButton.ImageNormalProperty, new BitmapImage(){ TextureAsset=normalAsset }),
                new Setter(ImageButton.ImageHoverProperty, new BitmapImage(){ TextureAsset=mouseOverAsset }),
                new Setter(ImageButton.ImagePressedProperty, new BitmapImage(){ TextureAsset=pressedAsset }),
                new Setter(ImageButton.ImageDisabledProperty, new BitmapImage(){ TextureAsset=disabledAsset }),
            };
        }
    }
}
