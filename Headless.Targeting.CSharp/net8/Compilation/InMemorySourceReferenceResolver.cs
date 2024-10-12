internal class InMemorySourceReferenceResolver(Dictionary<string, string>? scriptsByName = null) : SourceReferenceResolver
{
    private readonly SourceFileResolver _baseResolver = new([], AppContext.BaseDirectory);
    private readonly Dictionary<string, string> _inMemorySourceFiles = scriptsByName ?? [];

    public override string? NormalizePath(string path, string? baseFilePath) => _inMemorySourceFiles.ContainsKey(path) ? path : _baseResolver.NormalizePath(path, baseFilePath);
    public override string? ResolveReference(string path, string? baseFilePath) => _inMemorySourceFiles.ContainsKey(path) ? path : _baseResolver.ResolveReference(path, baseFilePath);
    public override Stream OpenRead(string resolvedPath) => _inMemorySourceFiles.TryGetValue(resolvedPath, out var content) ? new MemoryStream(Encoding.UTF8.GetBytes(content)) : _baseResolver.OpenRead(resolvedPath);

    protected bool Equals(InMemorySourceReferenceResolver other) => _inMemorySourceFiles.Equals(other._inMemorySourceFiles) && _baseResolver.Equals(other._baseResolver);
    public override bool Equals(object? obj) => obj is InMemorySourceReferenceResolver sr && (ReferenceEquals(this, obj) || Equals(sr));
    public override int GetHashCode() => unchecked(((37 * 397) ^ _inMemorySourceFiles.GetHashCode()) * 397) ^ _baseResolver.GetHashCode();
}