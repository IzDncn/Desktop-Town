using DesktopAdventures.Engine;
using static DesktopAdventures.Engine.Palette;

namespace DesktopAdventures.World;

/// <summary>
/// Prozedural erzeugte Landschaft über die volle Breite: sanfte Hügel, Gras, Erdschichten,
/// verstreute Steine, Pilze, Büsche und Blumen. Statische Teile werden einmal vorgerendert;
/// Grashalme und Blumen schwingen im Wind und werden pro Frame darübergezeichnet.
/// </summary>
public sealed class Landscape
{
    private const int GroundDepth = 20;   // mittlere Höhe des Bodens in Kunst-Pixeln
    private const int DecorCell = 30;     // Rastergröße für Dekor-Platzierung

    private readonly record struct Flower(int X, int BaseY, int Stem, uint Color);
    private readonly record struct Shading(uint Hi, uint Mid, uint Lo, uint Outline);

    private static readonly Shading BushShading = new(GrassHi, GrassMid, GrassLo, GrassDeep);
    private static readonly Shading StoneShading = new(StoneHi, StoneMid, StoneLo, StoneOutline);
    private static readonly Shading CapShading = new(CapHi, CapMid, CapLo, CapOutline);
    private static readonly uint[] FlowerColors = { FlowerRed, FlowerYellow, FlowerWhite, FlowerBlue, FlowerPink };

    private readonly int _seed;
    private readonly int[] _surface;
    private readonly PixelBuffer _static;
    private readonly List<Flower> _flowers = new();

    public int Width { get; }
    public int Height { get; }

    public Landscape(int width, int height, int seed)
    {
        Width = width;
        Height = height;
        _seed = seed;
        _surface = new int[width];
        _static = new PixelBuffer(width, height);

        BuildSurface();
        PaintGround();
        PlaceDecor();
    }

    /// <summary>Oberste Gras-Zeile an Spalte x (hier stehen später Häuser, Figuren usw.).</summary>
    public int SurfaceY(int x) => _surface[Math.Clamp(x, 0, Width - 1)];

    public void Render(PixelBuffer target, double timeSec)
    {
        target.CopyFrom(_static);
        DrawFlowers(target, timeSec);
        DrawBlades(target, timeSec);
    }

    // ---------- Gelände ----------

    private void BuildSurface()
    {
        for (int x = 0; x < Width; x++)
        {
            double offset = (Noise.Smooth(x / 90.0, _seed + 1) * 2 - 1) * 3.2
                          + (Noise.Smooth(x / 23.0, _seed + 2) * 2 - 1) * 1.0;
            _surface[x] = Height - GroundDepth + (int)Math.Round(offset);
        }
    }

    private void PaintGround()
    {
        for (int x = 0; x < Width; x++)
        {
            int grassDepth = 4 + (int)(Noise.Hash(x / 2, _seed + 11) % 3); // zottelige Unterkante, 2 px breit
            for (int y = _surface[x]; y < Height; y++)
                _static.Set(x, y, GroundColor(x, y, y - _surface[x], grassDepth));
        }
    }

    private uint GroundColor(int x, int y, int depth, int grassDepth)
    {
        bool checker = ((x + y) & 1) == 0;

        if (depth < grassDepth)
        {
            if (depth == grassDepth - 1 && depth > 3) return GrassDeep;
            return depth switch
            {
                0 => Noise.Hash(x, _seed + 5) % 4 == 0 ? GrassMid : GrassHi,
                1 => checker ? GrassHi : GrassMid,
                2 => GrassMid,
                3 => checker ? GrassMid : GrassLo,
                _ => GrassLo,
            };
        }

        int s = depth - grassDepth;
        uint color = s switch
        {
            < 2 => s == 0 || checker ? SoilHi : SoilMid,
            < 5 => s < 4 || !checker ? SoilMid : SoilLo,
            < 9 => SoilLo,
            _ => checker || s > 11 ? SoilDeep : SoilLo,
        };

        uint speck = Noise.Hash(x * 131 + y * 17, _seed + 9);
        if (speck % 23 == 0) return SoilHi;
        if (speck % 29 == 0) return SoilDeep;
        return color;
    }

    // ---------- Dekor ----------

    private void PlaceDecor()
    {
        for (int cell = 0; cell * DecorCell < Width + DecorCell; cell++)
        {
            uint h = Noise.Hash(cell, _seed + 100);
            int x = cell * DecorCell + (int)((h >> 8) % DecorCell);
            if (x >= Width) continue;

            int baseY = _surface[x];
            int roll = (int)(h % 100);

            if (roll < 28) PlaceFlowers(x, h);
            else if (roll < 38) DrawDome(x, baseY, 3 + (int)((h >> 12) % 3), 2 + (int)((h >> 14) % 2), StoneShading);
            else if (roll < 45) DrawMushroom(x, baseY);
            else if (roll < 58) DrawBush(x, baseY, 5 + (int)((h >> 12) % 4), 4 + (int)((h >> 14) % 3), h);
        }
    }

    private void PlaceFlowers(int x, uint h)
    {
        int count = 2 + (int)((h >> 16) % 3);
        for (int i = 0; i < count; i++)
        {
            int fx = x + i * 4 + (int)(Noise.Hash(i, (int)h) % 3);
            if (fx >= Width) break;
            int stem = 4 + (int)(Noise.Hash(fx, _seed + 3) % 3);
            uint color = FlowerColors[Noise.Hash(fx, _seed + 4) % FlowerColors.Length];
            _flowers.Add(new Flower(fx, _surface[fx], stem, color));
        }
    }

    private void DrawMushroom(int cx, int baseY)
    {
        for (int y = baseY - 2; y <= baseY; y++)
        {
            _static.Set(cx, y, MushroomStem);
            _static.Set(cx + 1, y, MushroomStem);
        }
        DrawDome(cx, baseY - 3, 3, 3, CapShading);
        _static.Set(cx - 1, baseY - 5, FlowerWhite);
        _static.Set(cx + 1, baseY - 6, FlowerWhite);
    }

    private void DrawBush(int cx, int baseY, int rx, int ry, uint h)
    {
        DrawDome(cx, baseY, rx, ry, BushShading);
        if (h % 5 != 0) return;

        // gelegentlich Beeren
        for (int i = 0; i < 3; i++)
        {
            uint b = Noise.Hash(i, (int)h);
            int bx = cx - rx / 2 + (int)(b % (uint)Math.Max(1, rx));
            int by = baseY - 1 - (int)((b >> 8) % (uint)Math.Max(1, ry - 2));
            _static.Set(bx, by, Berry);
        }
    }

    /// <summary>
    /// Halbkuppel (Busch, Stein, Pilzhut): Basis auf baseY, wächst nach oben. Licht von links oben,
    /// Schattierung mit Schachbrett-Dithering an den Stufen, dunkle Kontur außer am Boden.
    /// </summary>
    private void DrawDome(int cx, int baseY, int rx, int ry, Shading shading)
    {
        bool Inside(int x, int y)
        {
            double nx = (x - cx) / (rx + 0.5);
            double ny = (baseY - y + 0.5) / ry;
            return ny > 0 && nx * nx + ny * ny <= 1;
        }

        for (int y = baseY - ry; y <= baseY; y++)
        {
            for (int x = cx - rx - 1; x <= cx + rx + 1; x++)
            {
                if (!Inside(x, y)) continue;

                if (y < baseY && (!Inside(x - 1, y) || !Inside(x + 1, y) || !Inside(x, y - 1)))
                {
                    _static.Set(x, y, shading.Outline);
                    continue;
                }

                double nx = (x - cx) / (rx + 0.5);
                double ny = (baseY - y + 0.5) / ry;
                double light = ny * 0.7 - nx * 0.35 + (((x + y) & 1) == 0 ? -0.07 : 0.07);
                _static.Set(x, y, light > 0.6 ? shading.Hi : light > 0.3 ? shading.Mid : shading.Lo);
            }
        }
    }

    // ---------- Animation ----------

    private void DrawFlowers(PixelBuffer target, double t)
    {
        foreach (var f in _flowers)
        {
            int sway = (int)Math.Round(Wind.At(f.X, t) * 1.6);

            for (int k = 0; k < f.Stem; k++)
                target.Set(f.X + (k >= f.Stem / 2 ? sway : 0), f.BaseY - 1 - k, GrassLo);

            int hx = f.X + sway, hy = f.BaseY - 1 - f.Stem;
            target.Set(hx - 1, hy, f.Color);
            target.Set(hx + 1, hy, f.Color);
            target.Set(hx, hy - 1, f.Color);
            target.Set(hx, hy + 1, f.Color);
            target.Set(hx, hy, FlowerCenter);
        }
    }

    private void DrawBlades(PixelBuffer target, double t)
    {
        for (int x = 0; x < Width; x++)
        {
            uint h = Noise.Hash(x, _seed + 7);
            if (h % 100 >= 38) continue;

            int height = 2 + (int)((h >> 8) % 4);
            uint color = ((h >> 12) % 3) switch { 0 => GrassHi, 1 => GrassMid, _ => GrassLo };
            double sway = Wind.At(x, t) * 1.6;

            for (int k = 0; k < height; k++)
            {
                double fraction = (double)k / (height - 1);
                int dx = (int)Math.Round(sway * fraction * fraction); // oben stärker ausgelenkt
                target.Set(x + dx, _surface[x] - 1 - k, color);
            }
        }
    }
}
