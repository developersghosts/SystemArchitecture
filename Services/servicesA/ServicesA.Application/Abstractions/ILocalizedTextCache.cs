namespace ServicesA.Application.Abstractions;

/// <summary>Clears the in-memory message dictionary so the next message reads the database again.</summary>
public interface ILocalizedTextCache
{
    void Invalidate();
}
