using System;
using System.Collections.Generic;
using System.Windows;
using Mageki.Drawables;
using Mageki.TouchTracking;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using Wpf.Ui.Appearance;

namespace Mageki.WPF
{
    public class ControllerPanelWPF
    {
        #region Fields (copied from Xamarin ControllerPanel)

        private bool requireUpdate = false;
        private readonly Keyboard keyboard = new Keyboard();
        private readonly SideButton lSide = new SideButton()
        {
            Side = Side.Left,
            Color = SKColors.Pink,
        };
        private readonly SideButton rSide = new SideButton()
        {
            Side = Side.Right,
            Color = SKColors.Purple,
        };

        private readonly SquareButton lMenu = new SquareButton()
        {
            Color = ButtonColors.Red,
            BorderColor = new SKColor(0xFF880000),
        };
        private readonly SquareButton rMenu = new SquareButton()
        {
            Color = ButtonColors.Yellow,
            BorderColor = new SKColor(0xFF888800),
        };

        private readonly Lever lever = new Lever();
        private readonly MenuFrame lMenuFrame;
        private readonly MenuFrame rMenuFrame;
        private readonly Circles circles;
        private readonly IList<TouchableObject> touchableObjects;

        private int oldWidth = -1;
        private int oldHeight = -1;
        private bool inRhythmGame;

        private readonly SKElement _canvas;

        #endregion

        #region Constants (copied from Xamarin ControllerPanel)

        const float PanelPaddingRatio = 0.5f;
        const float LRSpacingCoef = 0.5f;
        const float KeyboardMarginTopCoef = 0.5f;
        const float ButtonSpacingCoef = 0.25f;
        const float MenuSizeCoef = 0.5f;
        const float MenuPaddingCoef = 1.125f;
        const float LeverWidth = 0.5f;

        #endregion

        public ControllerPanelWPF(SKElement canvas)
        {
            _canvas = canvas;
            ApplyCanvasTheme();

            circles = new Circles(keyboard);
            lMenuFrame = new MenuFrame(lMenu, Side.Left);
            rMenuFrame = new MenuFrame(rMenu, Side.Right);

            touchableObjects = new List<TouchableObject>
            {
                keyboard,
                lMenu,
                rMenu,
                lever,
                lSide,
                rSide,
            };

            Settings.ValueChanged += Settings_ValueChanged;
            StaticIO.OnLedChanged += StaticIO_OnLedChanged;
            StaticIO.OnStatusChanged += StaticIO_OnStatusChanged;

            SetLed(StaticIO.Colors);
        }

        private void StaticIO_OnLedChanged(object sender, EventArgs e)
        {
            var colors = (ButtonColors[])StaticIO.Colors.Clone();
            RunOnUiThread(() => SetLed(colors));
        }

        private void StaticIO_OnStatusChanged(object sender, OnStatusChangedEventArgs e)
        {
            RunOnUiThread(_canvas.InvalidateVisual);
        }

        private void RunOnUiThread(Action action)
        {
            if (_canvas.Dispatcher.HasShutdownStarted || _canvas.Dispatcher.HasShutdownFinished)
                return;

            if (_canvas.Dispatcher.CheckAccess())
                action();
            else
                _canvas.Dispatcher.BeginInvoke(action);
        }

        private void Settings_ValueChanged(string name)
        {
            if (name == nameof(Settings.CanvasTheme))
            {
                ApplyCanvasTheme();
                _canvas.InvalidateVisual();
                return;
            }
            if (
                name == nameof(Settings.ButtonBottomMargin)
                || name == nameof(Settings.HideGameButtons)
                || name == nameof(Settings.EnableCompositeMode)
                || name == nameof(Settings.HideWallActionDevices)
                || name == nameof(Settings.LeverMoveMode)
            )
            {
                requireUpdate = true;
                _canvas.InvalidateVisual();
            }
        }

        public void Draw(SKCanvas canvas, int width, int height)
        {
            ApplyCanvasTheme();
            canvas.Clear(CanvasPalette.Background);

            if (oldWidth != width || oldHeight != height || requireUpdate)
            {
                requireUpdate = false;
                UpdateLayout(width, height);
            }

            circles.Draw(canvas);
            lMenuFrame.Draw(canvas);
            rMenuFrame.Draw(canvas);
            lSide.Draw(canvas);
            rSide.Draw(canvas);
            lMenu.Draw(canvas);
            rMenu.Draw(canvas);
            lever.Draw(canvas);
            keyboard.Draw(canvas);

            oldWidth = width;
            oldHeight = height;
        }

        private static void ApplyCanvasTheme()
        {
            CanvasPalette.Apply(Settings.CanvasTheme, ApplicationThemeManager.IsMatchedDark());
        }

        private void UpdateLayout(int width, int height)
        {
            var nSide =
                BitConverter.GetBytes(keyboard.ShowLeft)[0]
                + BitConverter.GetBytes(keyboard.ShowRight)[0];

            float baseCoef =
                1
                / (
                    PanelPaddingRatio * 2
                    + LRSpacingCoef * (nSide / 2)
                    + ButtonSpacingCoef * nSide * 2
                    + nSide * 3
                );

            float baseLength = width * baseCoef;
            float buttonSideLength = baseLength;
            float menuSideLength = baseLength * MenuSizeCoef;
            float menuPadding = baseLength * MenuPaddingCoef;
            float keyboardMarginTop = baseLength * KeyboardMarginTopCoef;
            float bottomMargin = (height - baseLength) * Settings.ButtonBottomMargin;

            if (Settings.HideGameButtons)
            {
                bottomMargin = 0;
                buttonSideLength = 0;
            }

            keyboard.Padding = new SKPoint(baseLength * PanelPaddingRatio, 0);
            keyboard.Position = new SKPoint(0, height - bottomMargin - buttonSideLength);
            keyboard.Spacing = baseLength * LRSpacingCoef;
            keyboard.Size = new SKSize(width, height - keyboard.Position.Y);
            keyboard.Left.Spacing = keyboard.Right.Spacing = baseLength * ButtonSpacingCoef;
            keyboard.Visible = !Settings.HideGameButtons;

            lMenu.Size = rMenu.Size = new SKSize(menuSideLength, menuSideLength);
            lMenu.Position = new SKPoint(
                menuPadding,
                keyboard.BoundingBox.Top - keyboardMarginTop - menuSideLength * 2
            );
            rMenu.Position = new SKPoint(width - menuPadding - menuSideLength, lMenu.Position.Y);
            lMenu.Visible = rMenu.Visible = !Settings.HideMenuButtons;

            lSide.ButtonHeight = rSide.ButtonHeight = baseLength;
            lSide.Size = rSide.Size = new SKSize(width / 2f, height - keyboard.BoundingBox.Height);
            lSide.Position = new SKPoint(0, 0);
            rSide.Position = new SKPoint(width / 2f, 0);
            lSide.Padding = rSide.Padding = new SKPoint(0, keyboardMarginTop);
            lSide.Visible = rSide.Visible = !Settings.HideWallActionDevices;

            if (Settings.EnableCompositeMode || Settings.HideWallActionDevices)
            {
                lever.Size = new SKSize(width, lSide.Size.Height);
                lever.Position = new SKPoint(0, 0);
                lever.Padding = new SKPoint(
                    width * (1 - LeverWidth) / 2,
                    keyboard.BoundingBox.Top - lMenu.BoundingBox.Bottom
                );
            }
            else
            {
                lever.Size = new SKSize(width * LeverWidth, lSide.Size.Height);
                lever.Position = new SKPoint(width * (1 - LeverWidth) / 2, 0);
                lever.Padding = new SKPoint(0, keyboard.BoundingBox.Top - lMenu.BoundingBox.Bottom);
            }
        }

        public void OnTouchAction(long id, TouchActionType type, SKPoint pixelLocation)
        {
            switch (type)
            {
                case TouchActionType.Pressed:
                    foreach (var obj in touchableObjects)
                    {
                        if (
                            obj.Visible
                            && obj.HitTest(pixelLocation)
                            && obj.HandleTouchPressed(id, pixelLocation)
                        )
                            break;
                    }
                    break;

                case TouchActionType.Moved:
                    foreach (var obj in touchableObjects)
                    {
                        if (obj.HandleTouchMoved(id, pixelLocation))
                            break;
                    }
                    break;

                case TouchActionType.Released:
                    foreach (var obj in touchableObjects)
                        obj.HandleTouchReleased(id);
                    break;

                case TouchActionType.Cancelled:
                    foreach (var obj in touchableObjects)
                        obj.HandleTouchCancelled(id);
                    break;
            }

            UpdateIO();
        }

        private void UpdateIO()
        {
            ButtonBase[] buttons = new ButtonBase[]
            {
                keyboard[0],
                keyboard[1],
                keyboard[2],
                lSide,
                lMenu,
                keyboard[3],
                keyboard[4],
                keyboard[5],
                rSide,
                rMenu,
            };

            for (int i = 0; i < buttons.Length; i++)
            {
                if (StaticIO.Data.GameButtons[i] != buttons[i].TouchCount)
                    StaticIO.SetGameButton(i, buttons[i].TouchCount);
            }

            MoveLever(lever.Value);
        }

        private void MoveLever(float x)
        {
            short newValue = (short)(short.MaxValue * x);
            short oldValue = StaticIO.Data.Lever;
            var threshold = short.MaxValue / (Settings.LeverLinearity / 2f);

            if ((int)(newValue / threshold) != (int)(oldValue / threshold))
                StaticIO.SetLever(newValue);
        }

        public void SetLed(ButtonColors[] colors)
        {
            for (int i = 0; i < colors.Length; i++)
                keyboard[i].Color = colors[i];

            bool temp =
                keyboard.Left[0].Color == keyboard.Right[0].Color
                && keyboard.Left[1].Color == keyboard.Right[1].Color
                && keyboard.Left[2].Color == keyboard.Right[2].Color
                && keyboard.Left[0].Color == ButtonColors.Red
                && keyboard.Left[1].Color == ButtonColors.Green
                && keyboard.Left[2].Color == ButtonColors.Blue;

            inRhythmGame = temp;
            lMenu.Visible = rMenu.Visible = !inRhythmGame;
            keyboard.AntiMisTouch = Settings.AntiMisTouch && inRhythmGame;

            _canvas.InvalidateVisual();
        }
    }
}
