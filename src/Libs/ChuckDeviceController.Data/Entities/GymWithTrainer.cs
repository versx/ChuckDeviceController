namespace ChuckDeviceController.Data.Entities;

public class GymWithTrainer
{
    public Gym Gym { get; set; }

    public GymTrainer Trainer { get; set; }

    public GymWithTrainer(Gym gym, GymTrainer trainer)
    {
        Gym = gym;
        Trainer = trainer;
    }
}