/*
namespace ChuckDeviceController.Services.Routes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using ChuckDeviceController.Data.Entities;
    using ChuckDeviceController.Services.Routes.Walk;

    public delegate void UpdatePositionDelegate(double lat, double lng, double speed);

    public interface IEvent
    {
    }

    public class HumanWalkingEvent : IEvent
    {
        public double OldWalkingSpeed;
        public double CurrentWalkingSpeed;
    }

    public class Navigation
    {
        private readonly Random WalkingRandom = new Random();
        private List<IWalkStrategy> WalkStrategyQueue { get; set; }

        private bool _autoWalkAI;
        private double distance;
        //private int speedChangeFactor = 1;
        private int _AutoWalkDist;

        public Dictionary<Type, DateTime> WalkStrategyBlackList = new Dictionary<Type, DateTime>();

        public IWalkStrategy WalkStrategy { get; set; }

        public Navigation(ILogicSettings logicSettings)
        {
            InitializeWalkStrategies(logicSettings);
            WalkStrategy = GetStrategy(logicSettings);
        }

        public double VariantRandom(ISession session, double currentSpeed)
        {
            //
             * this changes as bug into BaseWalkStrategy
             * 
            double variantSpeed = session.LogicSettings.WalkingSpeedVariant;
            if (variantSpeed == 0.0)
                return currentSpeed;
            double baseSpeed = session.LogicSettings.WalkingSpeedInKilometerPerHour;
            // Between -1.0 and 1.0 the current deviation from baseSpeed
            double currentVariantFactor = (currentSpeed - baseSpeed) / variantSpeed;
            // The more speed is changing towards limit, the more it is likely that speed change direction changes 
            if (WalkingRandom.Next(1, 10) > 8
                || (currentVariantFactor * speedChangeFactor > 0.0
                    && WalkingRandom.NextDouble() + Math.Abs(currentVariantFactor) > 1.50))
                // Change from slow down to speed up or vice versa
                speedChangeFactor *= -1;
            // This is the max. delta for each speed change
            double newSpeed = currentSpeed + WalkingRandom.NextDouble() * variantSpeed * speedChangeFactor;
            var max = baseSpeed + variantSpeed;
            var min = baseSpeed - variantSpeed;
            if (newSpeed > max)
                newSpeed -= newSpeed - max;
            if (newSpeed < min)
                newSpeed += min - newSpeed;
            if (Math.Round(newSpeed, 2) != Math.Round(currentSpeed, 2))
            {
                session.EventDispatcher.Send(new HumanWalkingEvent
                {
                    OldWalkingSpeed = currentSpeed,
                    CurrentWalkingSpeed = newSpeed
                });
            }
            return newSpeed;
            //
            if (WalkingRandom.Next(1, 10) > 5)
            {
                if (WalkingRandom.Next(1, 10) > 5)
                {
                    var randomicSpeed = currentSpeed;
                    var max = session.LogicSettings.WalkingSpeedInKilometerPerHour +
                              session.LogicSettings.WalkingSpeedVariant;
                    randomicSpeed += WalkingRandom.NextDouble() * (0.02 - 0.001) + 0.001;

                    if (randomicSpeed > max)
                        randomicSpeed = max;

                    if (Math.Round(randomicSpeed, 2) != Math.Round(currentSpeed, 2))
                    {
                        session.EventDispatcher.Send(new HumanWalkingEvent
                        {
                            OldWalkingSpeed = currentSpeed,
                            CurrentWalkingSpeed = randomicSpeed
                        });
                    }
                    return randomicSpeed;
                }
                else
                {
                    var randomicSpeed = currentSpeed;
                    var min = session.LogicSettings.WalkingSpeedInKilometerPerHour -
                              session.LogicSettings.WalkingSpeedVariant;
                    randomicSpeed -= WalkingRandom.NextDouble() * (0.02 - 0.001) + 0.001;

                    if (randomicSpeed < min)
                        randomicSpeed = min;

                    if (Math.Round(randomicSpeed, 2) != Math.Round(currentSpeed, 2))
                    {
                        session.EventDispatcher.Send(new HumanWalkingEvent
                        {
                            OldWalkingSpeed = currentSpeed,
                            CurrentWalkingSpeed = randomicSpeed
                        });
                    }
                    return randomicSpeed;
                }
            }
            return currentSpeed;
        }

        public async Task Move(Coordinate targetLocation,
            Func<Task> functionExecutedWhileWalking,
            CancellationToken cancellationToken, double customWalkingSpeed = 0.0)
        {
            distance = LocationUtils.CalculateDistanceInMeters(session.Client.CurrentLatitude, session.Client.CurrentLongitude,
            targetLocation.Latitude, targetLocation.Longitude);

            cancellationToken.ThrowIfCancellationRequested();

            // If the stretegies become bigger, create a factory for easy management

            //Logging.Logger.Write($"Navigation - Walking speed {customWalkingSpeed}");
            InitializeWalkStrategies(session.LogicSettings);
            WalkStrategy = GetStrategy(session.LogicSettings);
            await WalkStrategy.Walk(targetLocation, functionExecutedWhileWalking, session, cancellationToken, customWalkingSpeed).ConfigureAwait(false);
        }

        private void InitializeWalkStrategies(ILogicSettings logicSettings)
        {
            //AutoWalkAI code
            _autoWalkAI = logicSettings.AutoWalkAI;
            _AutoWalkDist = logicSettings.AutoWalkDist;

            if (_autoWalkAI)
            {
                if (distance >= _AutoWalkDist)
                {
                    Logging.Logger.Write($"No Base walk strategy enabled, using 'NecroBot Walk'", Logging.LogLevel.Info, ConsoleColor.DarkYellow);
                }
                else if (distance > 15)
                {
                    Logging.Logger.Write($"Distance to travel is < {_AutoWalkDist}m, using 'NecroBot Walk'", Logging.LogLevel.Info, ConsoleColor.DarkYellow);
                }
            }

            WalkStrategyQueue = new List<IWalkStrategy>();

            //Maybe change configuration for a Navigation Type.
            if (logicSettings.DisableHumanWalking)
                WalkStrategyQueue.Add(new FlyStrategy());

            if (logicSettings.UseGpxPathing)
                WalkStrategyQueue.Add(new HumanPathWalkingStrategy());

            // This is the NecroBot Walk default
            WalkStrategyQueue.Add(new HumanStrategy());
        }

        public bool IsWalkingStrategyBlacklisted(Type strategy)
        {
            if (!WalkStrategyBlackList.ContainsKey(strategy))
                return false;

            var now = DateTime.Now;
            var blacklistExpiresAt = WalkStrategyBlackList[strategy];
            if (blacklistExpiresAt < now)
            {
                // Blacklist expired
                WalkStrategyBlackList.Remove(strategy);
                return false;
            }
            return true;
        }

        public void BlacklistStrategy(Type strategy)
        {
            // Black list for 1 hour.
            WalkStrategyBlackList[strategy] = DateTime.Now.AddHours(1);
        }

        public IWalkStrategy GetStrategy(ILogicSettings logicSettings)
        {
            return WalkStrategyQueue.First(q => !IsWalkingStrategyBlacklisted(q.GetType()));
        }
    }
}
*/