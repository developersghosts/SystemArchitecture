using ServicesA.Domain.Common;

namespace ServicesA.Domain.Entities.Identity;

public sealed class FullName : ValueObject
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;

    private FullName() { }

    private FullName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public static FullName Create(string firstName, string lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        return new FullName(firstName.Trim(), lastName.Trim());
    }

    public override string ToString() => $"{FirstName} {LastName}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
    }
}
