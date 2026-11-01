namespace RejestrCzasu.Features.TypyPracy;

/// <summary>Model of the Work type form (add and edit); <see cref="Id"/> is <c>null</c> when adding.</summary>
public sealed class WorkTypeForm
{
    public int? Id { get; set; }

    public string Name { get; set; } = "";
}
