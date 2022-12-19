﻿namespace ChuckDeviceController.Pvp.Models;

public class PvpRank
{
    public uint CompetitionRank { get; set; }

    public uint DenseRank { get; set; }

    public uint OrdinalRank { get; set; }

    public double Percentage { get; set; }

    public uint Cap { get; set; }

    public bool IsCapped { get; set; }

    public List<IvWithCp> IVs { get; set; } = new();

    public PvpRank()
    {
        CompetitionRank = 0;
        DenseRank = 0;
        OrdinalRank = 0;
        Percentage = 0;
        Cap = 0;
        IsCapped = false;
        IVs = new();
    }

    public class IvWithCp
    {
        public IV IV { get; set; }

        public double Level { get; set; }

        public uint CP { get; set; }

        public IvWithCp(IV iv, double level, uint cp)
        {
            IV = iv;
            Level = level;
            CP = cp;
        }
    }

    public override string ToString()
    {
        var ivs = IVs.Select(iv => $"{iv.IV.Attack}/{iv.IV.Defense}/{iv.IV.Stamina}");
        return string.Join(",", ivs);
    }
}