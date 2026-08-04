using System.Threading.Tasks;

namespace AvatarExplorer.UI.Interfaces;

public interface IPostInitializable
{
    Task OnInitialized();
}
