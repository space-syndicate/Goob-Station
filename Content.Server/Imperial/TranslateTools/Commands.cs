// using System.IO;
// using System.Linq;
// using Content.Server.Administration;
// using Content.Shared.Administration;
// using Robust.Shared.Console;
// using Robust.Shared.ContentPack;
// using Robust.Shared.Prototypes;
// using Robust.Shared.Utility;

// namespace Content.Server.Imperial.TranslateTools;

// [AdminCommand(AdminFlags.Host)]
// public sealed class GetLocalData : LocalizedCommands
// {
//     public string Description => Loc.GetString("callertcommand-desc");
//     public string Help => Loc.GetString("callertcommand-help");
//     [Dependency] private readonly IEntityManager _entity = default!;
//     [Dependency] private readonly IPrototypeManager _prototype = default!;
//     [Dependency] private readonly IResourceManager resourceManager = default!;
//     public override string Command => "getLocaleData";
//     private List<string> cultures = new()
//     {
//         "ru-RU",
//         "en-US"
//     };
//     private List<string> mode = new()
//     {
//         "list",
//         "table"
//     };

//     public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
//     {
//         if (args.Count() == 1)
//         {
//             return CompletionResult.FromOptions(cultures);
//         }
//         if (args.Count() == 2)
//         {
//             return CompletionResult.FromOptions(mode);
//         }

//         return CompletionResult.Empty;
//     }

//     public override void Execute(IConsoleShell shell, string argStr, string[] args)
//     {
//         if (args.Count() < 1) return;
//         var culture = args[0];
//         var root = new ResPath($"/Locale/{culture}/");

//         var files = resourceManager.ContentFindFiles(root)
//             .Where(c => c.Filename.EndsWith(".ftl", StringComparison.InvariantCultureIgnoreCase))
//             .ToArray();
//         var resources = files.AsParallel().Select(path =>
//         {
//             string contents;

//             using (var fileStream = resourceManager.ContentFileRead(path))
//             using (var reader = new StreamReader(fileStream, EncodingHelpers.UTF8))
//             {
//                 contents = reader.ReadToEnd();
//             }

//             var parser = new LinguiniParser(contents);
//             var resource = parser.Parse();
//             return (path, resource, contents);
//         });
//         List<string> result = new();
//         foreach (var (path, resource, content) in resources)
//         {
//             if (args.Count() == 1 || args.Count() == 2 && args[1] == "list")
//             {
//                 shell.WriteLine($"{path}");
//                 shell.WriteLine($"{content}");
//             }
//             if (args.Count() == 2 && args[1] == "table")
//             {
//                 var record = content.Split(new char[] { '\n', '\r' });
//                 foreach (var str in record)
//                 {
//                     if (str != "")
//                         result.Add($"{path}鎰{str}\n");
//                 }
//             }
//         }
//         var savePath = new ResPath($"{args[0]}").ToRootedPath();
//         resourceManager.UserData.CreateDir(savePath.Directory);
//         using var writer = resourceManager.UserData.OpenWriteText(savePath);
//         foreach (var str in result)
//             writer.Write(str);
//         shell.WriteLine("Complite!");
//         shell.WriteLine($"data load to file {args[0]} in data directory");
//     }
// }

// [AdminCommand(AdminFlags.Host)]
// public sealed class GetAllEntityes : LocalizedCommands
// {
//     public string Description => Loc.GetString("callertcommand-desc");
//     public string Help => Loc.GetString("callertcommand-help");
//     [Dependency] private readonly IEntityManager _entity = default!;
//     [Dependency] private readonly IPrototypeManager _prototype = default!;
//     [Dependency] private readonly IResourceManager resourceManager = default!;
//     [Dependency] private readonly ILocalizationManager _localizationManager = default!;
//     public override string Command => "getAllEntityes";

//     public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
//     {
//         if (args.Count() == 1)
//         {
//             return CompletionResult.FromHint($"file");
//         }
//         if (args.Count() == 2)
//         {
//             return CompletionResult.FromOptions(new List<string>() { "true", "false" });
//         }

//         return CompletionResult.Empty;
//     }

//     public override void Execute(IConsoleShell shell, string argStr, string[] args)
//     {
//         var query = _prototype.EnumeratePrototypes<EntityPrototype>();
//         List<string> result = new();
//         foreach (var record in query)
//         {
//             var data = _localizationManager.GetEntityData(record.ID);
//             if (_localizationManager.HasString($"ent-{record.ID}") && args[1] == "false" || record.Abstract || record.HideSpawnMenu) continue;
//             result.Add($"ent-{record.ID} = 鎰{data.Name}\n");
//             result.Add($".desc = 鎰{data.Desc}\n");
//             result.Add($".suffix = 鎰{data.Suffix}\n");
//         }
//         var savePath = new ResPath($"{args[0]}").ToRootedPath();
//         resourceManager.UserData.CreateDir(savePath.Directory);
//         using var writer = resourceManager.UserData.OpenWriteText(savePath);
//         foreach (var str in result)
//             writer.Write(str);
//         shell.WriteLine("Complite!");
//         shell.WriteLine($"data load to file {args[0]} in data directory");

//     }
// }

