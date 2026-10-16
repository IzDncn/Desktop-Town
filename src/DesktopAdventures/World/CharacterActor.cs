using DesktopAdventures.Characters;
using DesktopAdventures.Engine;

namespace DesktopAdventures.World;

/// <summary>Ein Charakter in der Welt: wandert zufällig, steht herum und reagiert auf Klicks.</summary>
public sealed class CharacterActor
{
    private enum Mode { Idle, Walk, React }

    private const double EdgePadding = 6;  // Abstand der Figur zum Bildschirmrand (Kunst-Pixel)
    private const int GroundSink = 1;      // Sohle ragt eine Zeile ins Gras

    private readonly CharacterProfile _profile;
    private readonly CharacterSheet _sheet;
    private readonly Random _random;
    private readonly double _minX, _maxX;

    private Mode _mode;
    private double _modeTime, _modeDuration, _animTime;
    private int _direction = 1; // 1 = nach rechts, -1 = nach links

    public CharacterProfile Profile => _profile;

    /// <summary>Mitte der Füße in Kunst-Pixeln.</summary>
    public double X { get; private set; }

    public CharacterActor(CharacterProfile profile, double startX, int seed, int worldWidth)
    {
        _profile = profile;
        _sheet = profile.Sheet;
        _random = new Random(seed);
        _minX = EdgePadding + _sheet.FrameWidth / 2.0;
        _maxX = Math.Max(_minX, worldWidth - EdgePadding - _sheet.FrameWidth / 2.0);
        X = Math.Clamp(startX, _minX, _maxX);
        _direction = _random.Next(2) == 0 ? -1 : 1;
        Enter(Mode.Idle);
    }

    public int FootY(Landscape landscape) => landscape.SurfaceY((int)Math.Round(X)) + GroundSink;

    public void Update(double dt)
    {
        _modeTime += dt;
        _animTime += dt;

        switch (_mode)
        {
            case Mode.Walk:
                X += _direction * _profile.Speed * dt;
                if (X <= _minX) { X = _minX; _direction = 1; }
                else if (X >= _maxX) { X = _maxX; _direction = -1; }
                if (_modeTime >= _modeDuration) Enter(Mode.Idle);
                break;

            case Mode.Idle:
                if (_modeTime >= _modeDuration) Enter(Mode.Walk);
                break;

            case Mode.React:
                if (_modeTime >= _modeDuration) Enter(Mode.Idle);
                break;
        }
    }

    public void Draw(PixelBuffer target, Landscape landscape)
    {
        var (destX, destY) = Origin(landscape);
        var clip = _sheet.Get(ClipName());
        int frame = (int)(_animTime * clip.Fps);
        frame = clip.Loop ? frame % clip.Frames : Math.Min(frame, clip.Frames - 1);

        target.Draw(_sheet.Image, frame * _sheet.FrameWidth, clip.Row * _sheet.FrameHeight,
            _sheet.FrameWidth, _sheet.FrameHeight, destX, destY, flipX: _direction < 0);
    }

    public bool Contains(int artX, int artY, Landscape landscape)
    {
        var (x, y) = Origin(landscape);
        return artX >= x && artX < x + _sheet.FrameWidth && artY >= y && artY < y + _sheet.FrameHeight;
    }

    /// <summary>Spielt die Reaktions-Animation ab (falls das Template eine hat).</summary>
    public void Poke()
    {
        if (_mode != Mode.React && _sheet.Has("react")) Enter(Mode.React);
    }

    private (int X, int Y) Origin(Landscape landscape) =>
        ((int)Math.Round(X) - _sheet.FrameWidth / 2, FootY(landscape) - _sheet.FrameHeight + 1);

    private string ClipName() => _mode switch { Mode.Walk => "walk", Mode.React => "react", _ => "idle" };

    private void Enter(Mode mode)
    {
        // Ohne Lauf-Animation oder mit Tempo 0 bleibt die Figur stehen.
        if (mode == Mode.Walk && (!_sheet.Has("walk") || _profile.Speed <= 0)) mode = Mode.Idle;

        _mode = mode;
        _modeTime = 0;
        _animTime = 0;

        switch (mode)
        {
            case Mode.Idle:
                _modeDuration = Between(_profile.IdleSeconds);
                break;
            case Mode.Walk:
                _modeDuration = Between(_profile.WalkSeconds);
                if (X < _minX + 40) _direction = 1;
                else if (X > _maxX - 40) _direction = -1;
                else if (_random.NextDouble() < 0.35) _direction = -_direction;
                break;
            case Mode.React:
                _modeDuration = _sheet.Get("react").Duration * 2; // zweimal abspielen
                break;
        }
    }

    private double Between((double Min, double Max) range) =>
        range.Min + _random.NextDouble() * (range.Max - range.Min);
}
