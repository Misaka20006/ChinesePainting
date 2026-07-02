using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class BulletManager : Node
{
    public static BulletManager Instance { get; private set; }

    [Export] public PackedScene playerBulletPrefab;
    [Export] public int playerBulletPoolSize = 50;

    [Export] public Godot.Collections.Array<EnemyBulletType> enemyBulletTypes = new Godot.Collections.Array<EnemyBulletType>();

    private Queue<Node2D> playerBulletPool = new Queue<Node2D>();
    private Dictionary<string, Queue<Node2D>> enemyBulletPools = new Dictionary<string, Queue<Node2D>>();
    private List<BulletController> activeBullets = new List<BulletController>();

    public override void _Ready()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePools();
        }
        else
        {
            QueueFree();
        }
    }

    private void InitializePools()
    {
        if (playerBulletPrefab != null)
        {
            for (int i = 0; i < playerBulletPoolSize; i++)
            {
                Node2D bullet = playerBulletPrefab.Instantiate<Node2D>();
                AddChild(bullet);
                bullet.Visible = false;
                bullet.SetProcess(false);
                playerBulletPool.Enqueue(bullet);
            }
        }

        foreach (var bulletType in enemyBulletTypes)
        {
            if (bulletType.bulletPrefab != null && !string.IsNullOrEmpty(bulletType.typeName))
            {
                Queue<Node2D> pool = new Queue<Node2D>();

                for (int i = 0; i < bulletType.poolSize; i++)
                {
                    Node2D bullet = bulletType.bulletPrefab.Instantiate<Node2D>();
                    AddChild(bullet);
                    bullet.Visible = false;
                    bullet.SetProcess(false);
                    pool.Enqueue(bullet);
                }

                enemyBulletPools[bulletType.typeName] = pool;
            }
        }
    }

    public BulletController SpawnPlayerBullet(Vector2 position, float rotation, Vector2 direction, float speed, int ownerID)
    {
        Node2D bullet = GetPlayerBulletFromPool();

        if (bullet != null)
        {
            bullet.GlobalPosition = position;
            bullet.Rotation = rotation;
            bullet.Visible = true;
            bullet.SetProcess(true);

            BulletController controller = bullet as BulletController;
            if (controller != null)
            {
                controller.Initialize(direction, speed, true, ownerID);
            }

            activeBullets.Add(controller);
            return controller;
        }

        return null;
    }

    public BulletController SpawnEnemyBullet(Vector2 position, float rotation, Vector2 direction, float speed, string bulletTypeName = "Default")
    {
        Node2D bullet = GetEnemyBulletFromPool(bulletTypeName);

        if (bullet != null)
        {
            bullet.GlobalPosition = position;
            bullet.Rotation = rotation;
            bullet.Visible = true;
            bullet.SetProcess(true);

            BulletController controller = bullet as BulletController;
            if (controller != null)
            {
                controller.Initialize(direction, speed, false, 0);
            }

            activeBullets.Add(controller);
            return controller;
        }

        return null;
    }

    public BulletController SpawnEnemyBulletWithDamage(Vector2 position, float rotation, Vector2 direction, float speed, int damage, string bulletTypeName = "Default")
    {
        BulletController controller = SpawnEnemyBullet(position, rotation, direction, speed, bulletTypeName);

        if (controller != null)
        {
            controller.SetDamage(damage);
        }

        return controller;
    }

    private Node2D GetPlayerBulletFromPool()
    {
        if (playerBulletPool.Count > 0)
        {
            return playerBulletPool.Dequeue();
        }

        if (playerBulletPrefab != null)
        {
            Node2D bullet = playerBulletPrefab.Instantiate<Node2D>();
            AddChild(bullet);
            bullet.Visible = false;
            bullet.SetProcess(false);
            return bullet;
        }

        return null;
    }

    private Node2D GetEnemyBulletFromPool(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            typeName = "Default";

        if (!enemyBulletPools.ContainsKey(typeName))
        {
            GD.PrintErr($"未找到子弹类型: {typeName}，使用默认类型");

            if (enemyBulletPools.Count > 0)
            {
                typeName = enemyBulletPools.Keys.ToArray()[0];
            }
            else
            {
                return null;
            }
        }

        Queue<Node2D> pool = enemyBulletPools[typeName];

        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }

        EnemyBulletType bulletType = null;
        foreach (var bt in enemyBulletTypes)
        {
            if (bt.typeName == typeName)
            {
                bulletType = bt;
                break;
            }
        }

        if (bulletType != null && bulletType.bulletPrefab != null)
        {
            Node2D bullet = bulletType.bulletPrefab.Instantiate<Node2D>();
            AddChild(bullet);
            bullet.Visible = false;
            bullet.SetProcess(false);
            pool.Enqueue(bullet);
            return pool.Dequeue();
        }

        return null;
    }

    public void ReturnBulletToPool(BulletController bullet, bool isPlayerBullet)
    {
        bullet.Visible = false;
        bullet.SetProcess(false);

        if (isPlayerBullet)
        {
            playerBulletPool.Enqueue(bullet);
        }
        else
        {
            if (!string.IsNullOrEmpty(bullet.BulletTypeName) && enemyBulletPools.ContainsKey(bullet.BulletTypeName))
            {
                enemyBulletPools[bullet.BulletTypeName].Enqueue(bullet);
            }
        }

        activeBullets.Remove(bullet);
    }

    public void ClearAllBullets()
    {
        foreach (BulletController bullet in activeBullets)
        {
            if (bullet != null)
            {
                bullet.Visible = false;
                bullet.SetProcess(false);

                bool isPlayerBullet = bullet.IsPlayerBullet;
                if (isPlayerBullet)
                {
                    playerBulletPool.Enqueue(bullet);
                }
                else
                {
                    string typeName = bullet.BulletTypeName;
                    if (!string.IsNullOrEmpty(typeName) && enemyBulletPools.ContainsKey(typeName))
                    {
                        enemyBulletPools[typeName].Enqueue(bullet);
                    }
                }
            }
        }

        activeBullets.Clear();
    }

    public void ClearEnemyBullets()
    {
        List<BulletController> toRemove = new List<BulletController>();

        foreach (BulletController bullet in activeBullets)
        {
            if (bullet != null && !bullet.IsPlayerBullet)
            {
                bullet.Visible = false;
                bullet.SetProcess(false);

                string typeName = bullet.BulletTypeName;
                if (!string.IsNullOrEmpty(typeName) && enemyBulletPools.ContainsKey(typeName))
                {
                    enemyBulletPools[typeName].Enqueue(bullet);
                }

                toRemove.Add(bullet);
            }
        }

        foreach (BulletController bullet in toRemove)
        {
            activeBullets.Remove(bullet);
        }
    }
}
