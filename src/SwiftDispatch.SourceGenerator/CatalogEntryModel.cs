using System.Collections.Immutable;

namespace SwiftDispatch.SourceGenerator;

/// <summary>Symbol-free identities and source hints frozen from the validated dispatch model.</summary>
internal sealed class CatalogTypeModel(string typeName, string? sourceLocation)
{
    public string TypeName { get; } = typeName;
    public string? SourceLocation { get; } = sourceLocation;
}

internal sealed class CatalogEntryModel(CatalogTypeModel message, string? response, string kind,
    ImmutableArray<CatalogTypeModel> behaviors, ImmutableArray<CatalogTypeModel> handlers,
    CatalogTypeModel? notificationStrategy)
{
    public CatalogTypeModel Message { get; } = message;
    public string? Response { get; } = response;
    public string Kind { get; } = kind;
    public ImmutableArray<CatalogTypeModel> Behaviors { get; } = behaviors;
    public ImmutableArray<CatalogTypeModel> Handlers { get; } = handlers;
    public CatalogTypeModel? NotificationStrategy { get; } = notificationStrategy;
}
