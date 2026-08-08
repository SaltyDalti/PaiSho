namespace PaiSho.Game
{
    /// <summary>When active, TileSelector completes actions instantly without animation or toasts.</summary>
    public static class HeadlessActionExecutor
    {
        public static bool IsActive { get; private set; }

        public static bool SkipPresentation => IsActive;

        public static void Begin()
        {
            IsActive = true;
        }

        public static void End()
        {
            IsActive = false;
        }
    }
}
