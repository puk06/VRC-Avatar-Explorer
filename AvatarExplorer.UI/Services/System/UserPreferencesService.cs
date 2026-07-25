namespace AvatarExplorer.UI.Services.System;

public class UserPreferencesService
{
    private static readonly UserPreferencesService _instance = new();
    public static UserPreferencesService Instance => _instance;

    public UserPreferencesRepository Repository { get; } = new();

    private UserPreferencesService()
    {
    }
}
