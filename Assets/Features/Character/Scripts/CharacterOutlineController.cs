internal sealed class CharacterOutlineController
{
    private const float HiddenWidth = 0f;
    private const float VisibleWidth = 1f;

    private readonly Outline _outline;

    public CharacterOutlineController(Outline outline) =>
        _outline = outline;

    public void Hide()
    {
        if (_outline != null)
            _outline.OutlineWidth = HiddenWidth;
    }

    public void Restore()
    {
        if (_outline != null)
            _outline.OutlineWidth = VisibleWidth;
    }
}
