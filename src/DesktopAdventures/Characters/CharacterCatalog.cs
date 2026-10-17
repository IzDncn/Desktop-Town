using System.IO;
using System.Text.Json;
using DesktopAdventures.Engine;

namespace DesktopAdventures.Characters;

/// <summary>
/// Lädt alle Charaktere aus Content/Characters/*.json. Ein Charakter = Template (Körper + Animationen)
/// + Palette (Farben) + optionale Ebenen (Hüte, Bart ...). Fehlerhafte Dateien werden übersprungen
/// und in <see cref="Errors"/> gemeldet, damit ein Tippfehler nie das ganze Spiel verhindert.
/// </summary>
public sealed class CharacterCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private sealed record LoadedTemplate(
        string Name, TemplateDefinition Definition, Sprite Sheet, Dictionary<string, Sprite> Layers);

    private readonly record struct ShadeSet(uint Hi, uint Mid, uint Lo);

    private readonly string _root;
    private readonly Dictionary<string, LoadedTemplate> _templates = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<CharacterProfile> _profiles = new();
    private readonly List<string> _errors = new();

    public IReadOnlyList<CharacterProfile> Profiles => _profiles;
    public IReadOnlyList<string> Errors => _errors;

    private CharacterCatalog(string contentRoot) => _root = contentRoot;

    public static CharacterCatalog Load(string contentRoot)
    {
        var catalog = new CharacterCatalog(contentRoot);
        catalog.LoadAll();
        return catalog;
    }

    private void LoadAll()
    {
        string dir = Path.Combine(_root, "Characters");
        if (!Directory.Exists(dir))
        {
            _errors.Add($"Ordner nicht gefunden: {dir}");
            return;
        }

        foreach (string file in Directory.EnumerateFiles(dir, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                _profiles.Add(LoadCharacter(file));
            }
            catch (Exception ex)
            {
                _errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }
    }

    // ---------- Charakter ----------

    private CharacterProfile LoadCharacter(string file)
    {
        var def = Read<CharacterDefinition>(file);
        if (string.IsNullOrWhiteSpace(def.Template))
            throw new InvalidDataException("\"template\" fehlt.");

        var template = GetTemplate(def.Template);
        var palette = ResolvePalette(template.Definition, def);

        var image = Recolor(template.Sheet, palette);
        foreach (string layerName in def.Layers ?? new List<string>())
        {
            if (!template.Layers.TryGetValue(layerName, out var layer))
                throw new InvalidDataException(
                    $"Template '{template.Name}' hat keine Ebene '{layerName}'. Vorhanden: {Join(template.Layers.Keys)}.");
            Composite(image, Recolor(layer, palette));
        }

        var frame = template.Definition.Frame;
        var clips = template.Definition.Animations.ToDictionary(
            a => a.Key, a => new AnimationClip(a.Value.Row, a.Value.Frames, a.Value.Fps, a.Value.Loop),
            StringComparer.OrdinalIgnoreCase);

        if (def.Extra != null)
            image = AppendExtra(image, clips, template, def.Extra, palette, Path.GetDirectoryName(file)!);

        var behavior = ResolveBehavior(template.Definition.Behavior, def.Behavior, clips);

        string id = Path.GetFileNameWithoutExtension(file);
        return new CharacterProfile
        {
            Id = id,
            Name = string.IsNullOrWhiteSpace(def.Name) ? id : def.Name,
            Sheet = new CharacterSheet(image, frame.Width, frame.Height, clips),
            Info = new CharacterInfo(
                def.Info?.Title ?? "",
                def.Info?.Description ?? "Über diesen Charakter ist noch nichts bekannt.",
                (IReadOnlyList<string>?)def.Info?.Facts ?? Array.Empty<string>(),
                def.Info?.UnlockHint ?? "Noch nicht freigeschaltet."),
            Speed = Math.Max(0, def.Speed),
            IdleSeconds = ParseRange(def.IdleSeconds, "idleSeconds"),
            WalkSeconds = ParseRange(def.WalkSeconds, "walkSeconds"),
            StartUnlocked = def.StartUnlocked,
            OnClick = behavior.OnClick,
            IdleActions = behavior.IdleActions,
            IdleActionChance = behavior.Chance,
        };
    }

    /// <summary>Hängt die Zusatz-Animationen eines einzelnen Charakters unter das Sheet.</summary>
    private static Sprite AppendExtra(Sprite image, Dictionary<string, AnimationClip> clips, LoadedTemplate template,
        ExtraAnimationsDefinition extra, Dictionary<string, ShadeSet> palette, string characterDir)
    {
        var frame = template.Definition.Frame;
        var extraSprite = Recolor(Sprite.Load(Path.Combine(characterDir, extra.Sheet)), palette);
        if (extraSprite.Height % frame.Height != 0)
            throw new InvalidDataException(
                $"extra.sheet: Höhe {extraSprite.Height} ist kein Vielfaches der Bildhöhe {frame.Height}.");

        int rowOffset = image.Height / frame.Height;
        foreach (var (name, a) in extra.Animations)
        {
            if (clips.ContainsKey(name))
                throw new InvalidDataException($"extra: Animation '{name}' gibt es im Template schon.");
            ValidateClip($"extra, Animation '{name}'", a, frame, extraSprite);
            clips[name] = new AnimationClip(a.Row + rowOffset, a.Frames, a.Fps, a.Loop);
        }

        var combined = new Sprite(Math.Max(image.Width, extraSprite.Width), image.Height + extraSprite.Height);
        CopyInto(combined, image, 0);
        CopyInto(combined, extraSprite, image.Height);
        return combined;
    }

    private static void CopyInto(Sprite target, Sprite source, int offsetY)
    {
        for (int y = 0; y < source.Height; y++)
            Array.Copy(source.Pixels, y * source.Width, target.Pixels, (y + offsetY) * target.Width, source.Width);
    }

    private static (IReadOnlyList<ActionChoice> OnClick, IReadOnlyList<ActionChoice> IdleActions, double Chance)
        ResolveBehavior(BehaviorDefinition? template, BehaviorDefinition? character, Dictionary<string, AnimationClip> clips)
    {
        var onClickDef = character?.OnClick ?? template?.OnClick;
        var idleDef = character?.IdleActions ?? template?.IdleActions;
        double chance = character?.IdleActionChance ?? template?.IdleActionChance ?? 0.35;

        // Ohne Angabe: Klick spielt "react", falls es die Animation gibt.
        onClickDef ??= clips.ContainsKey("react") ? new List<ActionDefinition> { new() { Animation = "react", Repeat = 2 } } : new();
        idleDef ??= new();

        if (chance is < 0 or > 1)
            throw new InvalidDataException("behavior.idleActionChance muss zwischen 0 und 1 liegen.");

        return (ToChoices("behavior.onClick", onClickDef, clips), ToChoices("behavior.idleActions", idleDef, clips), chance);
    }

    private static List<ActionChoice> ToChoices(string field, List<ActionDefinition> actions, Dictionary<string, AnimationClip> clips)
    {
        var result = new List<ActionChoice>();
        foreach (var a in actions)
        {
            if (!clips.ContainsKey(a.Animation))
                throw new InvalidDataException(
                    $"{field}: Animation '{a.Animation}' existiert nicht. Vorhanden: {Join(clips.Keys)}.");
            if (a.Weight <= 0 || a.Repeat < 1)
                throw new InvalidDataException($"{field}, '{a.Animation}': weight > 0 und repeat >= 1 nötig.");
            result.Add(new ActionChoice(a.Animation, a.Weight, a.Repeat));
        }
        return result;
    }

    private static (double Min, double Max) ParseRange(double[] values, string name)
    {
        if (values.Length != 2 || values[0] < 0 || values[1] < values[0])
            throw new InvalidDataException($"\"{name}\" muss [min, max] mit 0 <= min <= max sein.");
        return (values[0], values[1]);
    }

    // ---------- Palette ----------

    private static Dictionary<string, ShadeSet> ResolvePalette(TemplateDefinition template, CharacterDefinition def)
    {
        var slots = new Dictionary<string, SlotDefinition>(template.Slots, StringComparer.OrdinalIgnoreCase);

        if (def.Palette != null)
        {
            foreach (string key in def.Palette.Keys.Where(k => !slots.ContainsKey(k)))
                throw new InvalidDataException(
                    $"Unbekannter Palette-Eintrag '{key}'. Dieses Template kennt: {Join(slots.Keys)}.");
        }

        var byMarker = new Dictionary<string, ShadeSet>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, slot) in slots)
        {
            JsonElement? custom = null;
            if (def.Palette != null)
            {
                foreach (var (key, value) in def.Palette)
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase)) custom = value;
            }

            try
            {
                byMarker[slot.Marker] = custom is { } c ? ParseShades(c) : Derive(ColorUtil.Parse(slot.Default));
            }
            catch (FormatException ex)
            {
                throw new InvalidDataException($"Palette '{name}': {ex.Message}");
            }
        }
        return byMarker;
    }

    private static ShadeSet ParseShades(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                return Derive(ColorUtil.Parse(value.GetString()!));
            case JsonValueKind.Array when value.GetArrayLength() == 3:
                var c = value.EnumerateArray().Select(e => ColorUtil.Parse(e.GetString() ?? "")).ToArray();
                return new ShadeSet(c[0], c[1], c[2]);
            default:
                throw new FormatException("Farbe muss \"#rrggbb\" oder eine Liste [hell, mittel, dunkel] sein.");
        }
    }

    /// <summary>Leitet Licht- und Schattenton aus einer Grundfarbe ab (Schatten leicht ins Violette).</summary>
    private static ShadeSet Derive(uint mid) =>
        new(ColorUtil.Lerp(mid, 0xFFFFF2D0, 0.30), mid, ColorUtil.Lerp(mid, 0xFF2B1B3D, 0.35));

    private static Sprite Recolor(Sprite source, Dictionary<string, ShadeSet> palette)
    {
        var result = source.Clone();
        for (int i = 0; i < result.Pixels.Length; i++)
        {
            if (!SlotMarkers.TryGet(result.Pixels[i], out string family, out int shade)) continue;
            if (!palette.TryGetValue(family, out var set)) continue; // unbenutzter Marker bleibt sichtbar
            result.Pixels[i] = shade switch { 0 => set.Hi, 1 => set.Mid, _ => set.Lo };
        }
        return result;
    }

    private static void Composite(Sprite target, Sprite layer)
    {
        for (int i = 0; i < layer.Pixels.Length; i++)
            if (layer.Pixels[i] >> 24 != 0) target.Pixels[i] = layer.Pixels[i];
    }

    // ---------- Templates ----------

    private LoadedTemplate GetTemplate(string name)
    {
        if (_templates.TryGetValue(name, out var cached)) return cached;

        string dir = Path.Combine(_root, "Templates", name);
        if (!Directory.Exists(dir))
            throw new InvalidDataException($"Template '{name}' nicht gefunden (erwartet in {dir}).");

        var def = Read<TemplateDefinition>(Path.Combine(dir, "template.json"));
        var sheet = Sprite.Load(Path.Combine(dir, def.Sheet));
        Validate(name, def, sheet);

        var layers = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        foreach (var (layerName, relative) in def.Layers)
        {
            var layer = Sprite.Load(Path.Combine(dir, relative));
            if (layer.Width != sheet.Width || layer.Height != sheet.Height)
                throw new InvalidDataException(
                    $"Template '{name}', Ebene '{layerName}': Größe {layer.Width}x{layer.Height} passt nicht zum Sheet ({sheet.Width}x{sheet.Height}).");
            layers[layerName] = layer;
        }

        var loaded = new LoadedTemplate(name, def, sheet, layers);
        _templates[name] = loaded;
        return loaded;
    }

    private static void Validate(string name, TemplateDefinition def, Sprite sheet)
    {
        string prefix = $"Template '{name}': ";
        if (def.Frame.Width <= 0 || def.Frame.Height <= 0)
            throw new InvalidDataException(prefix + "\"frame\" braucht Breite und Höhe > 0.");
        if (!def.Animations.Keys.Contains("idle", StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException(prefix + "Animation \"idle\" fehlt.");

        if (sheet.Height % def.Frame.Height != 0)
            throw new InvalidDataException(prefix + $"Sheet-Höhe {sheet.Height} ist kein Vielfaches der Bildhöhe {def.Frame.Height}.");

        foreach (var (anim, a) in def.Animations)
            ValidateClip(prefix + $"Animation '{anim}'", a, def.Frame, sheet);

        foreach (var (slot, s) in def.Slots)
            if (!SlotMarkers.IsFamily(s.Marker))
                throw new InvalidDataException(prefix + $"Slot '{slot}': unbekannter Marker '{s.Marker}' (erlaubt: {Join(SlotMarkers.FamilyNames)}).");
    }

    private static void ValidateClip(string where, AnimationDefinition a, FrameSize frame, Sprite sheet)
    {
        if (a.Frames < 1 || a.Fps <= 0)
            throw new InvalidDataException($"{where}: frames >= 1 und fps > 0 nötig.");
        if (a.Row < 0 || (a.Row + 1) * frame.Height > sheet.Height || a.Frames * frame.Width > sheet.Width)
            throw new InvalidDataException($"{where} liegt außerhalb des Sheets ({sheet.Width}x{sheet.Height}).");
    }

    // ---------- Hilfen ----------

    private static T Read<T>(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"Datei nicht gefunden: {path}");
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions)
                   ?? throw new InvalidDataException("Datei ist leer.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Ungültiges JSON (Zeile {(ex.LineNumber ?? 0) + 1}): {ex.Message}");
        }
    }

    private static string Join(IEnumerable<string> values) => string.Join(", ", values.OrderBy(v => v));
}
