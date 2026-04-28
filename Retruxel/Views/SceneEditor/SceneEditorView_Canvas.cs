using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Retruxel.Core.Models;
using Retruxel.Core.Services;
using Retruxel.Services;

namespace Retruxel.Views;

public partial class SceneEditorView
{
    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(SceneCanvas);
        TxtCoordinates.Text = $"X: {(int)pos.X} | Y: {(int)pos.Y}";
    }

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        SelectElement(null);
    }

    private void Canvas_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = (e.Data.GetDataPresent(typeof(string)) || e.Data.GetDataPresent("RetruxelAssetDrop"))
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Canvas_Drop(object sender, DragEventArgs e)
    {
        var pos = e.GetPosition(SceneCanvas);
        var tileX = (int)pos.X / 8;
        var tileY = (int)pos.Y / 8;

        if (e.Data.GetData(typeof(string)) is string moduleId)
        {
            AddModuleToCanvas(moduleId, tileX, tileY);
            return;
        }

        if (e.Data.GetData("RetruxelAssetDrop") is AssetEntry asset)
            DropAssetOnCanvas(asset, tileX, tileY);
    }

    private Border BuildCanvasElement(SceneElement element)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x1E, 0x1E, 0x1E)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x8E, 0xFF, 0x71)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(0),
            Cursor = Cursors.Hand,
            Tag = element
        };

        var assetVisual = TryBuildAssetVisual(element);
        if (assetVisual is not null)
        {
            border.Child = assetVisual;
        }
        else
        {
            border.Padding = new Thickness(4, 2, 4, 2);
            border.Child = BuildModuleLabel(element);
        }

        border.MouseLeftButtonDown += (s, e) =>
        {
            if (e.ClickCount == 2)
            {
                _isDragging = false;
                _draggedElement = null;
                border.ReleaseMouseCapture();
                OpenVisualToolForElement(element);
                e.Handled = true;
                return;
            }

            SelectElement(element);
            _isDragging = true;
            _dragStartTileX = element.TileX;
            _dragStartTileY = element.TileY;
            var clickPos = e.GetPosition(border);
            _dragOffset = clickPos;
            _draggedElement = element;
            border.CaptureMouse();
            e.Handled = true;
        };

        border.MouseMove += (s, e) =>
        {
            if (_isDragging && _draggedElement == element)
            {
                var canvasPos = e.GetPosition(SceneCanvas);
                var adjustedX = canvasPos.X - _dragOffset.X;
                var adjustedY = canvasPos.Y - _dragOffset.Y;

                bool isLogicModule = _moduleRegistry?.LogicModules.ContainsKey(element.ModuleId) ?? false;
                
                int tileX, tileY;
                if (isLogicModule)
                {
                    tileX = (int)(adjustedX / 8);
                    tileY = (int)(adjustedY / 8);
                }
                else
                {
                    var maxTileX = (int)(SceneCanvas.Width / 8) - 1;
                    var maxTileY = (int)(SceneCanvas.Height / 8) - 1;
                    tileX = Math.Clamp((int)(adjustedX / 8), 0, maxTileX);
                    tileY = Math.Clamp((int)(adjustedY / 8), 0, maxTileY);
                }

                Canvas.SetLeft(border, tileX * 8);
                Canvas.SetTop(border, tileY * 8);

                element.TileX = tileX;
                element.TileY = tileY;

                UpdateModulePosition(element.Module, tileX, tileY);

                if (_selectedElement == element)
                    BuildPropertiesPanel(element);
            }
        };

        border.MouseLeftButtonUp += (s, e) =>
        {
            if (_isDragging && _draggedElement == element)
            {
                _isDragging = false;
                _draggedElement = null;
                border.ReleaseMouseCapture();

                var endTileX = element.TileX;
                var endTileY = element.TileY;

                if (endTileX != _dragStartTileX || endTileY != _dragStartTileY)
                {
                    var startX = _dragStartTileX;
                    var startY = _dragStartTileY;
                    var displayName = element.Module is Core.Interfaces.IModule m ? m.DisplayName : element.ModuleId;

                    var change = new StateChange
                    {
                        Description = $"Move {displayName}",
                        Type = ChangeType.Small,
                        Execute = () => { },
                        IsUndoable = true,
                        UndoCommand = new MoveElementCommand(
                            description: $"Move {displayName}",
                            moveTo: (tx, ty) =>
                            {
                                element.TileX = tx;
                                element.TileY = ty;
                                Canvas.SetLeft(border, tx * 8);
                                Canvas.SetTop(border, ty * 8);
                                UpdateModulePosition(element.Module, tx, ty);
                            },
                            prevX: startX, prevY: startY,
                            newX: endTileX, newY: endTileY)
                    };

                    _stateManager?.RegisterChange(change);
                }

                e.Handled = true;
            }
        };

        var contextMenu = new ContextMenu();
        var menuEdit = new MenuItem { Header = "Edit" };
        var menuDelete = new MenuItem { Header = "Delete" };

        menuEdit.Click += (_, _) => OpenVisualToolForElement(element);
        menuDelete.Click += (_, _) => RemoveElement(element);

        if (element.Module is Core.Interfaces.IModule mod && !string.IsNullOrEmpty(mod.VisualToolId))
            contextMenu.Items.Add(menuEdit);

        contextMenu.Items.Add(menuDelete);
        border.ContextMenu = contextMenu;

        element.CanvasVisual = border;
        return border;
    }

    private void AddModuleToCanvas(string moduleId, int tileX, int tileY)
    {
        var element = CreateSceneElement(moduleId, tileX, tileY);
        var displayName = element.Module is Core.Interfaces.IModule m ? m.DisplayName : moduleId;

        var change = new StateChange
        {
            Description = $"Add {displayName}",
            Type = ChangeType.Large,
            Execute = () =>
            {
                _elements.Add(element);

                var visual = BuildCanvasElement(element);
                Canvas.SetLeft(visual, tileX * 8);
                Canvas.SetTop(visual, tileY * 8);
                SceneCanvas.Children.Add(visual);
                element.CanvasVisual = visual;

                AddModuleToEvent("OnStart", moduleId, element);
                SelectElement(element);
                RefreshModulePalette();
                
                if (element.Module is Core.Interfaces.IModule mod && !string.IsNullOrEmpty(mod.VisualToolId))
                {
                    Dispatcher.InvokeAsync(() => OpenVisualToolForElement(element), 
                        System.Windows.Threading.DispatcherPriority.Background);
                }
            },
            IsUndoable = true,
            UndoCommand = new AddElementCommand(
                description: $"Add {displayName}",
                add: () =>
                {
                    _elements.Add(element);
                    var visual = BuildCanvasElement(element);
                    Canvas.SetLeft(visual, tileX * 8);
                    Canvas.SetTop(visual, tileY * 8);
                    SceneCanvas.Children.Add(visual);
                    element.CanvasVisual = visual;
                    AddModuleToEvent("OnStart", moduleId, element);
                    SelectElement(element);
                    RefreshModulePalette();
                },
                remove: () =>
                {
                    RemoveElementCore(element);
                    RefreshModulePalette();
                }
            )
        };

        _stateManager?.ApplyChange(change);
    }
}
