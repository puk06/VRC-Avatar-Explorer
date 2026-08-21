namespace AvatarExplorer.UI.Services.System;

public sealed class UserPreferencesService
{
    public static UserPreferencesService Instance { get; } = new();

    public UserPreferencesRepository Repository { get; } = new();

    private UserPreferencesService()
    {
    }
}
