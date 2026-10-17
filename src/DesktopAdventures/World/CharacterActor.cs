using DesktopAdventures.Characters;
using DesktopAdventures.Engine;

namespace DesktopAdventures.World;

/// <summary>
/// Ein Charakter in der Welt: wandert zufällig, steht herum, spielt gelegentlich Idle-Aktionen
/// und reagiert auf Klicks. Welche Aktionen es gibt, steht im Verhalten des Charakters (JSON).
/// </summary>
public sealed class CharacterActor
{
    private enum Mode { Idle, Walk, Action }

    private const double EdgePadding = 6;  // Abstand der Figur zum Bildschirmrand (Kunst-Pixel)
    private const int GroundSink = 1;      // Sohle ragt eine Zeile ins Gras

    private readonly CharacterProfile _profile;
    private readonly CharacterSheet _sheet;
    private readonly Random _random;
    private readonly double _minX, _maxX;

    private Mode _mode;
    private AnimationClip _clip;
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
        EnterIdle();
    }

    public int FootY(Landscape landscape) => landscape.SurfaceY((int)Math.Round(X)) + GroundSink;

    public void Update(double dt)
    {
        _modeTime += dt;
        _animTime += dt;
        if (_modeTime < _modeDuration && _mode != Mode.Walk) return;

        switch (_mode)
        {
            case Mode.Walk:
                X += _direction * _profile.Speed * dt;
                if (X <= _minX) { X = _minX; _direction = 1; }
                else if (X >= _maxX) { X = _maxX; _direction = -1; }
                if (_modeTime >= _modeDuration) EnterIdle();
                break;

            case Mode.Idle:
                if (_profile.IdleActions.Count > 0 && _random.NextDouble() < _profile.IdleActionChance)
                    StartAction(Pick(_profile.IdleActions));
                else
                    EnterWalk();
                break;

            case Mode.Action:
                EnterIdle();
                break;
        }
    }

    public void Draw(PixelBuffer target, Landscape landscape)
    {
        var (destX, destY) = Origin(landscape);
        int frame = (int)(_animTime * _clip.Fps);
        frame = _clip.Loop ? frame % _clip.Frames : Math.Min(frame, _clip.Frames - 1);

        target.Draw(_sheet.Image, frame * _sheet.FrameWidth, _clip.Row * _sheet.FrameHeight,
            _sheet.FrameWidth, _sheet.FrameHeight, destX, destY, flipX: _direction < 0);
    }

    public bool Contains(int artX, int artY, Landscape landscape)
    {
        var (x, y) = Origin(landscape);
        return artX >= x && artX < x + _sheet.FrameWidth && artY >= y && artY < y + _sheet.FrameHeight;
    }

    /// <summary>Klick auf die Figur: spielt eine Aktion aus dem Verhalten (onClick) ab, sofern gerade keine läuft.</summary>
    public void Poke()
    {
        if (_mode == Mode.Action || _profile.OnClick.Count == 0) return;
        StartAction(Pick(_profile.OnClick));
    }

    private (int X, int Y) Origin(Landscape landscape) =>
        ((int)Math.Round(X) - _sheet.FrameWidth / 2, FootY(landscape) - _sheet.FrameHeight + 1);

    private void EnterIdle()
    {
        SetMode(Mode.Idle, _sheet.Get("idle"), Between(_profile.IdleSeconds));
    }

    private void EnterWalk()
    {
        // Ohne Lauf-Animation oder mit Tempo 0 bleibt die Figur stehen.
        if (!_sheet.Has("walk") || _profile.Speed <= 0) { EnterIdle(); return; }

        if (X < _minX + 40) _direction = 1;
        else if (X > _maxX - 40) _direction = -1;
        else if (_random.NextDouble() < 0.35) _direction = -_direction;

        SetMode(Mode.Walk, _sheet.Get("walk"), Between(_profile.WalkSeconds));
    }

    private void StartAction(ActionChoice action)
    {
        var clip = _sheet.Get(action.Animation);
        SetMode(Mode.Action, clip, clip.Duration * action.Repeat);
    }

    private void SetMode(Mode mode, AnimationClip clip, double duration)
    {
        _mode = mode;
        _clip = clip;
        _modeDuration = duration;
        _modeTime = 0;
        _animTime = 0;
    }

    private ActionChoice Pick(IReadOnlyList<ActionChoice> choices)
    {
        double roll = _random.NextDouble() * choices.Sum(c => c.Weight);
        foreach (var choice in choices)
        {
            roll -= choice.Weight;
            if (roll <= 0) return choice;
        }
        return choices[^1];
    }

    private double Between((double Min, double Max) range) =>
        range.Min + _random.NextDouble() * (range.Max - range.Min);
}
