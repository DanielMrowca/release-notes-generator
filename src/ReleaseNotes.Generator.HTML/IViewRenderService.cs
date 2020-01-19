using System.Threading.Tasks;

namespace ReleaseNotes.Generator.HTML
{
    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync(string viewName, object model, bool partial = false);
    }
}