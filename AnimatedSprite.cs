using System.Windows.Controls;

namespace DesktopAdventures;

/// <summary>Spielt eine Animation auf einem Image-Control ab. Wird vom Game-Loop getaktet.</summary>
public sealed class AnimatedSprite
{
    private readonly Image _image;
    private Animation? _animation;
    private int _frame;
    private double _elapsedMs;

    /// <summary>Wird ausgeloest, wenn eine nicht-loopende Animation das Ende erreicht.</summary>
    public event Action? Finished;

    public AnimatedSprite(Image image) => _image = image;

    public void Play(Animation animation)
    {
        _animation = animation;
        _frame = 0;
        _elapsedMs = 0;
        _image.Source = animation.Frames[0];
    }

    public void Update(double dtMs)
    {
        if (_animation is null) return;

        _elapsedMs += dtMs;
        while (_elapsedMs >= _animation.FrameMs)
        {
            _elapsedMs -= _animation.FrameMs;
            _frame++;

            if (_frame >= _animation.Frames.Length)
            {
                if (_animation.Loop)
                {
                    _frame = 0;
                }
                else
                {
                    _frame = _animation.Frames.Length - 1;
                    Finished?.Invoke();
                    return; // Handler kann Play() aufgerufen haben
                }
            }
            _image.Source = _animation.Frames[_frame];
        }
    }
}
