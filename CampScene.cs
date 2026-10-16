using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using DesktopAdventures.Data;

namespace DesktopAdventures;

/// <summary>
/// Das Lager: Feuer plus alle Charaktere aus Characters/. Baut die Szene auf
/// und gibt den Takt (Update) an alle Beteiligten weiter.
/// </summary>
public sealed class CampScene
{
    private readonly AnimatedSprite _fire;
    private readonly List<Character> _characters = new();

    public IReadOnlyList<Character> Characters => _characters;

    /// <summary>Meldungen zu Charakteren, die nicht geladen werden konnten.</summary>
    public IReadOnlyList<string> LoadErrors { get; }

    public CampScene(Canvas sceneLayer, Canvas overlayLayer)
    {
        // --- Kulisse (unterste Ebene): Rahmen, Himmel, Boden ---
        var backdrop = new Image
        {
            Width = SceneLayout.Width,
            Height = SceneLayout.Height,
            Stretch = Stretch.Fill,
            Source = Animation.LoadAssetImage("backdrop.png")
        };
        RenderOptions.SetBitmapScalingMode(backdrop, BitmapScalingMode.NearestNeighbor);
        sceneLayer.Children.Add(backdrop);

        // --- Feuer ---
        double fireSize = 16 * SceneLayout.Scale;
        var fireImage = new Image { Width = fireSize, Height = fireSize, Stretch = Stretch.Fill };
        RenderOptions.SetBitmapScalingMode(fireImage, BitmapScalingMode.NearestNeighbor);
        Canvas.SetLeft(fireImage, SceneLayout.FireX);
        Canvas.SetTop(fireImage, SceneLayout.FloorY - fireSize);
        sceneLayer.Children.Add(fireImage);

        _fire = new AnimatedSprite(fireImage);
        _fire.Play(Animation.Load("fire.png", 16, 16, 120, loop: true));

        // --- Charaktere ---
        var result = CharacterLoader.LoadAll(
            Path.Combine(AppContext.BaseDirectory, "Characters"), SceneLayout.Seats.Length);

        var errors = result.Errors.ToList();
        foreach (var loaded in result.Characters)
        {
            try
            {
                AddCharacter(loaded, sceneLayer, overlayLayer);
            }
            catch (Exception ex)
            {
                errors.Add($"[{loaded.Id}] {ex.Message}");
            }
        }
        LoadErrors = errors;
    }

    private void AddCharacter(LoadedCharacter loaded, Canvas sceneLayer, Canvas overlayLayer)
    {
        var def = loaded.Definition;

        // Erst alle Sprites laden - schlaegt etwas fehl, wird nichts halb in die Szene gesetzt.
        var outfits = new Dictionary<string, OutfitAnimations>();
        foreach (var (name, outfit) in def.Outfits)
        {
            outfits[name] = new OutfitAnimations(
                Animation.LoadFromFile(CharacterLoader.ResolvePath(loaded.Folder, outfit.Idle),
                    def.FrameWidth, def.FrameHeight, def.IdleFrameMs, loop: true),
                Animation.LoadFromFile(CharacterLoader.ResolvePath(loaded.Folder, outfit.React),
                    def.FrameWidth, def.FrameHeight, def.ReactFrameMs, loop: false));
        }

        var view = new CharacterView(sceneLayer, overlayLayer, SceneLayout.Seats[def.Seat],
                                     def.FrameWidth, def.FrameHeight);
        var character = new Character(def, view.Sprite, outfits);

        view.Clicked += character.OnClicked;
        character.Said += view.ShowBubble;
        _characters.Add(character);
    }

    public void Update(double dtMs)
    {
        _fire.Update(dtMs);
        foreach (var character in _characters)
            character.Update(dtMs);
    }
}
