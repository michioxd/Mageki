using System.Collections.Generic;
using SkiaSharp;
using Xamarin.Essentials;

namespace Mageki.Drawables
{
    public abstract class ButtonBase : Box
    {
        public static Dictionary<ButtonColors, SKColor> Colors { get; } =
            new Dictionary<ButtonColors, SKColor>()
            {
                { ButtonColors.Red, new SKColor(0xFFFF455B) },
                { ButtonColors.Green, new SKColor(0xFF45FF75) },
                { ButtonColors.Blue, new SKColor(0xFF4589FF) },
                { ButtonColors.Yellow, new SKColor(0xFFFFD545) },
                { ButtonColors.Cyan, new SKColor(0xFF45F8FF) },
                { ButtonColors.Purple, new SKColor(0xFF8B45FF) },
                { ButtonColors.Blank, new SKColor(0xFFDDDDDD) },
                { ButtonColors.White, new SKColor(0xFFFFFFFF) },
            };

        public byte TouchCount
        {
            get => GetValue((byte)0);
            set
            {
                if (Settings.EnableHapticFeedback && TouchCount != value)
                {
                    HapticFeedback.Perform(HapticFeedbackType.Click);
                }
                SetValueWithNotify(value);
            }
        }

        public ButtonBase()
            : base() { }

        public override bool HandleTouchPressed(long id, SKPoint point)
        {
            // Guard against duplicate press events (can happen with WM_POINTER / stylus on Tablet PC)
            if (touchPoints.ContainsKey(id))
                return true;
            touchPoints.Add(id, point);
            TouchCount++;
            return base.HandleTouchPressed(id, point);
        }

        public override bool HandleTouchMoved(long id, SKPoint point)
        {
            return base.HandleTouchMoved(id, point);
        }

        public override void HandleTouchReleased(long id)
        {
            if (touchPoints.ContainsKey(id))
            {
                TouchCount--;
            }
            base.HandleTouchReleased(id);
        }
    }
}
