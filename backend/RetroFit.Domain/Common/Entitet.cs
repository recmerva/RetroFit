namespace RetroFit.Domain.Common;

/// <summary>
/// Bazna klasa za sve entitete sa celobrojnim primarnim ključem (IDENTITY u SQL Serveru).
/// </summary>
public abstract class Entitet
{
    public int Id { get; protected set; }
}
