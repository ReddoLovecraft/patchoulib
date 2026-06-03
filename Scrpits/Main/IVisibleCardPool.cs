namespace Patchouib.Scrpits.Main
{
    public interface IVisibleCardPool
    {
        string GetCardLibraryIconPath();

        string? GetCardLibraryHoverTipKey() => null;
    }

    public interface IVisbleCardPool : IVisibleCardPool
    {
    }
}
