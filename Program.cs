using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MiniRaycaster
{
    const int screenWidth = 800;
    const int screenHeight = 600;

    // ==== MAPA ====
    static int[,] map = new int[,]
    {
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
        {1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1},
        {1,0,1,0,1,0,1,0,1,0,1,0,1,0,0,1},
        {1,0,1,0,0,0,0,0,0,0,1,0,0,1,0,1},
        {1,0,0,0,1,0,1,0,1,0,1,0,1,0,0,1},
        {1,0,1,0,0,0,1,0,1,0,0,0,1,0,0,1},
        {1,0,1,0,1,0,0,0,0,0,1,0,1,0,0,1},
        {1,0,0,0,0,0,1,0,1,0,0,0,0,1,0,1},
        {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}
    };

    // ==== PLAYER ====
    static float posX = 3.5f, posY = 3.5f;
    static float dirX = -1f, dirY = 0f;
    static float planeX = 0f, planeY = 0.66f;

    // ==== MELEE ====
    static bool isAttacking = false;
    static float attackCooldown = 0f;

    // ==== TEXTURA DO INIMIGO ====
    static Texture2D enemyTexture;

    // ==== INIMIGOS ====
    class Enemy
    {
        public float x, y;
        public int hp;
        public bool alive => hp > 0;
    }

    static List<Enemy> enemies = new List<Enemy>()
    {
        new Enemy(){ x = 5.5f, y = 5.5f, hp = 3 },
        new Enemy(){ x = 7.5f, y = 2.5f, hp = 3 }
    };

    // ----------------------------------------------------------------------
    // MAIN
    // ----------------------------------------------------------------------

    public static void Main()
    {
        Raylib.InitWindow(screenWidth, screenHeight, "Raycaster .NET 10 (Sprites + Melee)");
        Raylib.SetTargetFPS(60);

        enemyTexture = Raylib.LoadTexture("C:/Users/gabri/source/repos/MiniRaycaster/enemy1.png");

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();

            UpdatePlayer();
            UpdateAttack(dt);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.DarkBlue);

            Render3D();
            DrawEnemies3D();
            DrawMiniMap();
            DrawWeapon();

            Raylib.EndDrawing();
        }

        Raylib.UnloadTexture(enemyTexture);
        Raylib.CloseWindow();
    }

    // ----------------------------------------------------------------------
    // PLAYER + MOVIMENTO
    // ----------------------------------------------------------------------

    static void UpdatePlayer()
    {
        float moveSpeed = 3f * Raylib.GetFrameTime();
        float rotSpeed = 2f * Raylib.GetFrameTime();

        bool forward = Raylib.IsKeyDown(KeyboardKey.W);
        bool back = Raylib.IsKeyDown(KeyboardKey.S);
        bool right = Raylib.IsKeyDown(KeyboardKey.D);
        bool left = Raylib.IsKeyDown(KeyboardKey.A);

        if (forward)
        {
            if (map[(int)posY, (int)(posX + dirX * moveSpeed)] == 0) posX += dirX * moveSpeed;
            if (map[(int)(posY + dirY * moveSpeed), (int)posX] == 0) posY += dirY * moveSpeed;
        }
        if (back)
        {
            if (map[(int)posY, (int)(posX - dirX * moveSpeed)] == 0) posX -= dirX * moveSpeed;
            if (map[(int)(posY - dirY * moveSpeed), (int)posX] == 0) posY -= dirY * moveSpeed;
        }

        if (right)
        {
            float sx = dirY;
            float sy = -dirX;
            if (map[(int)posY, (int)(posX + sx * moveSpeed)] == 0) posX += sx * moveSpeed;
            if (map[(int)(posY + sy * moveSpeed), (int)posX] == 0) posY += sy * moveSpeed;
        }

        if (left)
        {
            float sx = -dirY;
            float sy = dirX;
            if (map[(int)posY, (int)(posX + sx * moveSpeed)] == 0) posX += sx * moveSpeed;
            if (map[(int)(posY + sy * moveSpeed), (int)posX] == 0) posY += sy * moveSpeed;
        }

        if (Raylib.IsKeyDown(KeyboardKey.Right)) Rotate(-rotSpeed);
        if (Raylib.IsKeyDown(KeyboardKey.Left)) Rotate(rotSpeed);
    }

    static void Rotate(float angle)
    {
        float odx = dirX;
        dirX = dirX * MathF.Cos(angle) - dirY * MathF.Sin(angle);
        dirY = odx * MathF.Sin(angle) + dirY * MathF.Cos(angle);

        float opx = planeX;
        planeX = planeX * MathF.Cos(angle) - planeY * MathF.Sin(angle);
        planeY = opx * MathF.Sin(angle) + planeY * MathF.Cos(angle);
    }

    // ----------------------------------------------------------------------
    // ATAQUE MELEE
    // ----------------------------------------------------------------------

    static void UpdateAttack(float dt)
    {
        if (attackCooldown > 0)
            attackCooldown -= dt;

        if (Raylib.IsKeyPressed(KeyboardKey.Space) && attackCooldown <= 0)
        {
            isAttacking = true;
            attackCooldown = 0.35f;

            foreach (var e in enemies)
            {
                if (!e.alive) continue;

                float dx = e.x - posX;
                float dy = e.y - posY;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                if (dist < 1.3f)
                {
                    float dot = dx * dirX + dy * dirY;
                    if (dot > 0)
                    {
                        e.hp -= 1;
                    }
                }
            }
        }

        if (isAttacking && attackCooldown < 0.2f)
            isAttacking = false;
    }

    // ----------------------------------------------------------------------
    // RAYCASTER 3D
    // ----------------------------------------------------------------------

    static void Render3D()
    {
        for (int x = 0; x < screenWidth; x++)
        {
            float cameraX = 2f * x / screenWidth - 1f;
            float rayDirX = dirX + planeX * cameraX;
            float rayDirY = dirY + planeY * cameraX;

            int mapX = (int)posX;
            int mapY = (int)posY;

            float deltaDistX = (rayDirX == 0) ? 1e30f : MathF.Abs(1f / rayDirX);
            float deltaDistY = (rayDirY == 0) ? 1e30f : MathF.Abs(1f / rayDirY);

            float sideDistX, sideDistY;
            int stepX, stepY;
            int side = 0;
            bool hit = false;

            if (rayDirX < 0)
            {
                stepX = -1;
                sideDistX = (posX - mapX) * deltaDistX;
            }
            else
            {
                stepX = 1;
                sideDistX = (mapX + 1f - posX) * deltaDistX;
            }

            if (rayDirY < 0)
            {
                stepY = -1;
                sideDistY = (posY - mapY) * deltaDistY;
            }
            else
            {
                stepY = 1;
                sideDistY = (mapY + 1f - posY) * deltaDistY;
            }

            while (!hit)
            {
                if (sideDistX < sideDistY)
                {
                    sideDistX += deltaDistX;
                    mapX += stepX;
                    side = 0;
                }
                else
                {
                    sideDistY += deltaDistY;
                    mapY += stepY;
                    side = 1;
                }

                if (map[mapY, mapX] > 0) hit = true;
            }

            float perpWallDist = (side == 0)
                ? (sideDistX - deltaDistX)
                : (sideDistY - deltaDistY);

            int lineHeight = (int)(screenHeight / perpWallDist);

            int ds = -lineHeight / 2 + screenHeight / 2;
            int de = lineHeight / 2 + screenHeight / 2;

            Raylib.DrawLine(x, ds, x, de, side == 0 ? Color.LightGray : Color.DarkGray);
        }
    }

    // ----------------------------------------------------------------------
    // ENEMY BILLBOARD (AQUI É O QUE FAZ O SPRITE APARECER)
    // ----------------------------------------------------------------------

    static void DrawEnemies3D()
    {
        List<(Enemy e, float dist, float tx, float ty)> list = new();

        foreach (var e in enemies)
        {
            if (!e.alive) continue;

            float sx = e.x - posX;
            float sy = e.y - posY;
            float dist = sx * sx + sy * sy;

            float invDet = 1f / (planeX * dirY - dirX * planeY);
            float tx = invDet * (dirY * sx - dirX * sy);
            float ty = invDet * (-planeY * sx + planeX * sy);

            if (ty > 0)
                list.Add((e, dist, tx, ty));
        }

        var sorted = list.OrderByDescending(v => v.dist);

        foreach (var v in sorted)
        {
            float screenX = (screenWidth / 2f) * (1f + v.tx / v.ty);

            int size = (int)(screenHeight / v.ty);
            int w = size;
            int h = size;

            int drawX = (int)(screenX - w / 2);
            int drawY = screenHeight / 2 - h / 2;

            Rectangle src = new Rectangle(0, 0, enemyTexture.Width, enemyTexture.Height);
            Rectangle dst = new Rectangle(drawX, drawY, w, h);

            Raylib.DrawTexturePro(enemyTexture, src, dst, new Vector2(0, 0), 0f, Color.White);

            Raylib.DrawRectangle(drawX, drawY - 8, (int)(w * (v.e.hp / 3f)), 5, Color.Red);
        }
    }

    // ----------------------------------------------------------------------
    // MINI MAP
    // ----------------------------------------------------------------------

    static void DrawMiniMap()
    {
        int t = 12;
        for (int y = 0; y < map.GetLength(0); y++)
        {
            for (int x = 0; x < map.GetLength(1); x++)
            {
                Raylib.DrawRectangle(x * t, y * t, t - 1, t - 1, map[y, x] == 1 ? Color.Brown : Color.Black);
            }
        }

        Raylib.DrawCircle((int)(posX * t), (int)(posY * t), 4, Color.Yellow);

        foreach (var e in enemies)
        {
            if (e.alive)
                Raylib.DrawCircle((int)(e.x * t), (int)(e.y * t), 4, Color.Red);
        }
    }

    // ----------------------------------------------------------------------
    // ARMA (TELA)
    // ----------------------------------------------------------------------

    static void DrawWeapon()
    {
        Color c = isAttacking ? Color.Orange : Color.White;
        Raylib.DrawRectangle(screenWidth / 2 - 40, screenHeight - 120, 80, 80, c);
    }
}
