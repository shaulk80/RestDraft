using RestDraft.Domain.Results;

namespace RestDraft.Domain.Collections;

public static class ChildNameRules
{
    private const string NewFolderBaseName = "New folder";

    public static Result ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(ResultErrors.Validation("Name must not be empty."));
        }

        return Result.Success();
    }

    public static Result ValidateChildName(string name, Folder parent, Guid? renamingId)
    {
        var nameResult = ValidateName(name);
        if (!nameResult.IsSuccess)
        {
            return nameResult;
        }

        var trimmed = name.Trim();
        if (IsTaken(trimmed, ChildNames(parent, renamingId).ToList()))
        {
            return Result.Failure(ResultErrors.Validation($"A folder or request named '{trimmed}' already exists here."));
        }

        return Result.Success();
    }

    public static string NextAvailableName(string baseName, Folder parent)
    {
        return NextAvailableName(baseName, ChildNames(parent, null).ToList());
    }

    // The name as it is when no other name taken is the same, else with the lowest number from 2 that is free.
    public static string NextAvailableName(string baseName, IReadOnlyCollection<string> takenNames)
    {
        var candidate = baseName;
        for (var suffix = 2; IsTaken(candidate, takenNames); suffix++)
        {
            candidate = $"{baseName} {suffix}";
        }

        return candidate;
    }

    public static bool IsSameName(string first, string second)
    {
        return string.Equals(first.Trim(), second.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public static string NewFolderName(Folder parent)
    {
        return NextAvailableName(NewFolderBaseName, parent);
    }

    private static bool IsTaken(string name, IReadOnlyCollection<string> takenNames)
    {
        return takenNames.Any(taken => IsSameName(taken, name));
    }

    private static IEnumerable<string> ChildNames(Folder parent, Guid? renamingId)
    {
        return parent.Folders
            .Where(folder => folder.Id != renamingId)
            .Select(folder => folder.Name)
            .Concat(parent.Requests
                .Where(request => request.Id != renamingId)
                .Select(request => request.Name));
    }
}
