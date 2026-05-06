using Retruxel.Tool.SpriteEditor.Models;
using System.Windows;

namespace Retruxel.Tool.SpriteEditor.Views;

public partial class HitboxTypeDialog : Window
{
    public string BoxName { get; private set; } = "Box";
    public HitboxType BoxType { get; private set; } = HitboxType.Hurtbox;

    public HitboxTypeDialog()
    {
        InitializeComponent();
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        BoxName = TxtName.Text;
        BoxType = CmbType.SelectedIndex switch
        {
            0 => HitboxType.Hitbox,
            1 => HitboxType.Hurtbox,
            2 => HitboxType.Solidbox,
            _ => HitboxType.Hurtbox
        };

        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
