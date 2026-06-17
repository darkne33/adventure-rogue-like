using UnityEngine;

public interface ICursorService
{
    void ShowGameplayCursor();
    void ShowUiCursor();
}

public sealed class CursorService : ICursorService
{
    public void ShowGameplayCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void ShowUiCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
