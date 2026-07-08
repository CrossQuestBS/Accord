namespace Eccord;


public class User
{
    private readonly string _name;
    private readonly string _country;
    private int creditScore = 100;

    public User(string name, string country)
    {
        _name = name;
        _country = country;
        // Generated

    }
    
    public string Name => _name;
    public string Country => _country;




    public int CreditScore()
    {
        return creditScore;
    }

    public override string ToString()
    {
        return $"{_name} in {_country} has credit score: {CreditScore()}";
    }
}