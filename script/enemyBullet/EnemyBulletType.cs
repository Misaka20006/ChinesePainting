using Godot;

[GlobalClass]
public partial class EnemyBulletType : Resource
{
    [Export] public string typeName = "Default";
    [Export] public PackedScene bulletPrefab;
    [Export] public int poolSize = 30;
}
