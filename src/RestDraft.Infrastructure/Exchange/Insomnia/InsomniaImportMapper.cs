using RestDraft.Application.Abstractions;
using RestDraft.Application.Exchange;
using RestDraft.Domain.Collections;
using RestDraft.Infrastructure.Exchange.Insomnia.Dto;

namespace RestDraft.Infrastructure.Exchange.Insomnia;

// Turns an Insomnia collection into a RestDraft one. Siblings are put in the order of their sort keys, and a name that a
// sibling already has gets a number, because RestDraft allows each name once per folder.
internal sealed class InsomniaImportMapper
{
    private const string UntitledName = "Untitled";
    private const string DefaultCollectionName = "Imported collection";

    private readonly IIdGenerator _ids;

    public InsomniaImportMapper(IIdGenerator ids)
    {
        _ids = ids;
    }

    public ExchangeDocument Map(InsomniaCollectionDto dto)
    {
        var log = new ImportWarningLog();
        CountDropped(dto, log);
        var name = string.IsNullOrWhiteSpace(dto.Name) ? DefaultCollectionName : dto.Name.Trim();
        var root = MapFolder(_ids.NewId(), string.Empty, dto.Collection ?? [], string.Empty, log);
        return new ExchangeDocument(new Collection(_ids.NewId(), name, root), log.ToList());
    }

    private static void CountDropped(InsomniaCollectionDto dto, ImportWarningLog log)
    {
        if (dto.Environments is not null)
        {
            log.Add("The environments of the file are not imported.");
        }

        if (dto.CookieJar is not null)
        {
            log.Add("The cookies of the file are not imported.");
        }

        if (!string.IsNullOrEmpty(dto.Meta?.Description))
        {
            log.ItemsWithDescription++;
        }
    }

    private Folder MapFolder(Guid id, string name, List<InsomniaItemDto> items, string path, ImportWarningLog log)
    {
        var folders = new List<Folder>();
        var requests = new List<RequestDefinition>();
        var takenNames = new List<string>();
        var requestReader = new InsomniaRequestReader(log);
        foreach (var item in items.OrderBy(item => item.Meta?.SortKey ?? double.MaxValue))
        {
            var itemPath = ItemPath(path, item);
            var uniqueName = UniqueName(item, takenNames, itemPath, log);
            if (item.IsFolder)
            {
                CountFolderDropped(item, log);
                folders.Add(MapFolder(_ids.NewId(), uniqueName, item.Children!, itemPath, log));
                takenNames.Add(uniqueName);
                continue;
            }

            var request = requestReader.Read(item, _ids.NewId(), uniqueName, itemPath);
            if (request is not null)
            {
                requests.Add(request);
                takenNames.Add(uniqueName);
            }
        }

        return new Folder(id, name, folders, requests);
    }

    private static void CountFolderDropped(InsomniaItemDto folder, ImportWarningLog log)
    {
        if (folder.Headers is { Count: > 0 } || folder.Authentication is not null)
        {
            log.FoldersHaveHeadersOrAuth = true;
        }

        if (folder.Scripts is not null)
        {
            log.ItemsWithScripts++;
        }

        if (!string.IsNullOrEmpty(folder.Meta?.Description))
        {
            log.ItemsWithDescription++;
        }
    }

    private static string UniqueName(InsomniaItemDto item, List<string> takenNames, string path, ImportWarningLog log)
    {
        var name = string.IsNullOrWhiteSpace(item.Name) ? UntitledName : item.Name.Trim();
        var unique = ChildNameRules.NextAvailableName(name, takenNames);
        if (unique != name)
        {
            log.Add($"Renamed to '{unique}' because the name is already used in the same folder.", path);
        }

        return unique;
    }

    private static string ItemPath(string parentPath, InsomniaItemDto item)
    {
        var name = string.IsNullOrWhiteSpace(item.Name) ? UntitledName : item.Name.Trim();
        return parentPath.Length == 0 ? name : $"{parentPath} / {name}";
    }
}
