namespace RejestrCzasu.Data;

/// <summary>State of an Account (AD-7, AD-15), stored as text in <c>users.status</c>.</summary>
public enum AccountStatus
{
    Pending,
    Active,
    Blocked,
}
