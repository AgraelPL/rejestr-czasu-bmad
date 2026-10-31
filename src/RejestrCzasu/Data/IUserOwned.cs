namespace RejestrCzasu.Data;

/// <summary>
/// An entity owned by one User (AD-16). <see cref="AppDbContext"/> filters and assigns <see cref="UserId"/>
/// for every entity implementing this interface; screens never set or filter it themselves.
/// </summary>
public interface IUserOwned
{
    int UserId { get; set; }
}
