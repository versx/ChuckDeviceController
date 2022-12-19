namespace ChuckDeviceController.Pvp.Models;

public struct IV : IEquatable<IV>, IComparable<IV>
{
    private static List<IV>? _allCombinations;

    public ushort Attack { get; set; }

    public ushort Defense { get; set; }

    public ushort Stamina { get; set; }

    public IV(ushort attack, ushort defense, ushort stamina)
    {
        Attack = attack;
        Defense = defense;
        Stamina = stamina;
    }

    public static List<IV> GetAllCombinations()
    {
        if (_allCombinations == null)
        {
            var list = new List<IV>();
            for (ushort atk = 0; atk < 15; atk++)
            {
                for (ushort def = 0; def < 15; def++)
                {
                    for (ushort sta = 0; sta < 15; sta++)
                    {
                        list.Add(new IV(atk, def, sta));
                    }
                }
            }
            _allCombinations = list;
        }
        return _allCombinations;
    }

    public static IV GetHundoCombination() => new(15, 15, 15);

    public bool Equals(IV other)
    {
        return Attack == other.Attack &&
               Defense == other.Defense &&
               Stamina == other.Stamina;
    }

    public int CompareTo(IV other)
    {
        return Equals(other) ? 0 : -1;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;

        var other = (IV)obj;
        return Attack == other.Attack &&
               Defense == other.Defense &&
               Stamina == other.Stamina;
    }

    public override int GetHashCode() => Attack ^ Defense ^ Stamina;

    public static bool operator ==(IV left, IV right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(IV left, IV right)
    {
        return !(left == right);
    }

    public static bool operator <(IV left, IV right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(IV left, IV right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(IV left, IV right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(IV left, IV right)
    {
        return left.CompareTo(right) >= 0;
    }

    public override string ToString()
    {
        return $"{Attack}/{Defense}/{Stamina}";
    }
}