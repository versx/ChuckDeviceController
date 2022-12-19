namespace ChuckDeviceController.Data.Entities;

public class GymWithDefender
{
    public Gym Gym { get; set; }

    public GymDefender Defender { get; set; }

    public GymWithDefender(Gym gym, GymDefender defender)
    {
        Gym = gym;
        Defender = defender;
    }
}