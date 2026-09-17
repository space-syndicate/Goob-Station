using Content.Shared.Roles;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Client._CorvaxGoob.Chat;

public sealed partial class ChatHighlightsSystem : EntitySystem
{
    [Dependency] private IResourceManager _resourceMan = default!;
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    private ISawmill _logger = default!;

    private const string HighlightsPath = "/highlights.yml";
    private Dictionary<ProtoId<JobPrototype>, string> Highlights { get; set; } = new();

    public override void Initialize()
    {
        base.Initialize();

        _logger = Logger.GetSawmill("input");

        UpdateHighlights();
    }

    public void UpdateHighlights()
    {
        var path = new ResPath(HighlightsPath);
        if (_resourceMan.UserData.Exists(path))
        {
            try
            {
                LoadHighlightsFile(path);
            }
            catch (Exception e)
            {
                _logger.Error("Failed to load user highlights: " + e);
            }
        }
    }

    private void LoadHighlightsFile(ResPath path)
    {
        Highlights.Clear();

        System.IO.TextReader reader;
        reader = _resourceMan.UserData.OpenText(path);

        var documents = DataNodeParser.ParseYamlStream(reader).First();
        var mapping = (MappingDataNode) documents.Root;

        if (mapping.TryGet("highlights", out var highlightsRegsNode))
        {
            var baseKeyRegs = _serialization.Read<JobHighlightsRegistration[]>(highlightsRegsNode, notNullableOverride: false);

            foreach (var reg in baseKeyRegs)
            {
                if (reg.Key is null || !_proto.TryIndex<JobPrototype>(reg.Key, out var jobPrototype))
                    continue;

                if (reg.Value is null || reg.Value.Length == 0 || reg.Value.Length > 128)
                    continue;

                Highlights.Add(jobPrototype, reg.Value);
            }
        }
    }

    public void SaveHighlightsData(ProtoId<JobPrototype> protoId, string highlights)
    {
        try
        {
            if (!Highlights.ContainsKey(protoId))
                Highlights.Add(protoId, highlights);
            else
                Highlights[protoId] = highlights;

            var path = new ResPath(HighlightsPath);
            using var writer = _resourceMan.UserData.OpenWriteText(path);

            foreach (var toRemove in Highlights.Where(x => x.Value == ""))
                Highlights.Remove(toRemove.Key);

            if (Highlights.Count != 0)
                writer.WriteLine("highlights:");

            foreach (var highlight in Highlights)
            {
                writer.WriteLine("  - Key:" + highlight.Key);
                writer.WriteLine("    Value: \"" + highlight.Value + "\"");
            }
        }
        catch (Exception e)
        {
            _logger.Error("Failed to save user highlights: " + e);
        }
    }

    public bool TryGetSavedHighlights(ProtoId<JobPrototype> protoId, [NotNullWhen(true)] out string? highlights)
    {
        highlights = null;

        if (Highlights.TryGetValue(protoId, out highlights))
            return true;

        return false;
    }

    [DataDefinition]
    public sealed partial class JobHighlightsRegistration
    {
        [DataField("Key")]
        public string? Key;

        [DataField("Value")]
        public string? Value;
    }
}
