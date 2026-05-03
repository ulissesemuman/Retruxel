using System.Windows;

namespace Retruxel.Tool.SpriteEditor;

public partial class SpriteEditorWindow
{
    private void NotifySceneEditorUpdate()
    {
        if (_sceneEditor == null)
            return;

        var sceneEditorType = _sceneEditor.GetType();
        var updateMethod = sceneEditorType.GetMethod("RefreshModulePreview");
        
        if (updateMethod != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                updateMethod.Invoke(_sceneEditor, null);
            });
        }
    }

    private void OnSpriteChanged()
    {
        RenderCanvas();
        RenderPreview();
        NotifySceneEditorUpdate();
    }
}
