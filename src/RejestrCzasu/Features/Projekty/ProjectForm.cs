namespace RejestrCzasu.Features.Projekty;

/// <summary>Model of the Project form (add and edit); <see cref="Id"/> is <c>null</c> when adding.</summary>
public sealed class ProjectForm
{
    public int? Id { get; set; }

    public string Code { get; set; } = "";

    public string Name { get; set; } = "";
}
