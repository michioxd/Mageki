using SkiaSharp;

namespace Mageki.Drawables
{
    public static class CanvasPalette
    {
        public static bool IsDark { get; private set; }

        public static SKColor Background => IsDark ? new SKColor(0xFF202124) : SKColors.White;
        public static SKColor KeyboardPoint =>
            IsDark ? new SKColor(0xFFE6E6E6) : new SKColor(0xFF000000);
        public static SKColor KeyboardPointStroke =>
            IsDark ? new SKColor(0xFF303134) : SKColors.White;
        public static SKColor MenuStrong => IsDark ? new SKColor(0xFFE6E6E6) : SKColors.White;
        public static SKColor Medium => IsDark ? new SKColor(0xFFB8B8B8) : new SKColor(0xFF888888);
        public static SKColor Soft => IsDark ? new SKColor(0xFF777777) : new SKColor(0xFFAAAAAA);
        public static SKColor LeverSide =>
            IsDark ? new SKColor(0xFF303134) : new SKColor(0xFFD0D0D0);
        public static SKColor LeverHighlight => IsDark ? new SKColor(0xFFE6E6E6) : SKColors.White;
        public static SKColor LeverBorder =>
            IsDark ? new SKColor(0xFFB8B8B8) : new SKColor(0xFF222222);
        public static SKColor LeverHole =>
            IsDark ? new SKColor(0xFF888888) : new SKColor(0xFF666666);
        public static SKColor LeverBack => IsDark ? new SKColor(0xFF303134) : SKColors.White;

        public static void Apply(Mageki.CanvasTheme theme, bool systemIsDark)
        {
            IsDark = theme == CanvasTheme.Dark || (theme == CanvasTheme.Auto && systemIsDark);
        }
    }
}
