/*
namespace ChuckDeviceController.Services.Routes.Walk
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using ChuckDeviceController.Data.Entities;

    internal class HumanPathWalkingStrategy : BaseWalkStrategy
    {
        private double CurrentWalkingSpeed = 0;

        public HumanPathWalkingStrategy() : base()
        {
        }

        public override string RouteName => "NecroBot GPX";

        public override async Task Walk(Coordinate targetLocation, Func<Task> functionExecutedWhileWalking, CancellationToken cancellationToken, double walkSpeed = 0.0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var destinaionCoordinate = new Coordinate(targetLocation.Latitude, targetLocation.Longitude);
            //PlayerUpdateResponse result = null;

            if (CurrentWalkingSpeed <= 0)
                CurrentWalkingSpeed = session.LogicSettings.WalkingSpeedInKilometerPerHour;
            if (session.LogicSettings.UseWalkingSpeedVariant && walkSpeed == 0)
                CurrentWalkingSpeed = session.Navigation.VariantRandom(session, CurrentWalkingSpeed);

            var rw = new Random();
            var speedInMetersPerSecond = (walkSpeed > 0 ? walkSpeed : CurrentWalkingSpeed) / 3.6;
            var sourceLocation = new Coordinate(_client.CurrentLatitude, _client.CurrentLongitude);
            LocationUtils.CalculateDistanceInMeters(sourceLocation, destinaionCoordinate);
            var nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, destinaionCoordinate);
            var nextWaypointDistance = speedInMetersPerSecond;
            var waypoint = await LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing).ConfigureAwait(false);
            var requestSendDateTime = DateTime.Now;
            var requestVariantDateTime = DateTime.Now;

            await LocationUtils.UpdatePlayerLocationWithAltitude(session, waypoint, (float)speedInMetersPerSecond).ConfigureAwait(false);

            double SpeedVariantSec = rw.Next(1000, 10000);
            base.DoUpdatePositionEvent(session, waypoint.Latitude, waypoint.Longitude, walkSpeed, CurrentWalkingSpeed);

            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                var millisecondsUntilGetUpdatePlayerLocationResponse =
                    (DateTime.Now - requestSendDateTime).TotalMilliseconds;
                var millisecondsUntilVariant =
                    (DateTime.Now - requestVariantDateTime).TotalMilliseconds;

                sourceLocation = new Coordinate(_client.CurrentLatitude, _client.CurrentLongitude);
                var currentDistanceToTarget = LocationUtils
                    .CalculateDistanceInMeters(sourceLocation, destinaionCoordinate);

                //if (currentDistanceToTarget < 40)
                //{
                //    if (speedInMetersPerSecond > SpeedDownTo)
                //    {
                //        //Logger.Write("We are within 40 meters of the target. Speeding down to 10 km/h to not pass the target.", LogLevel.Info);
                //        speedInMetersPerSecond = SpeedDownTo;
                //    }
                //}

                if (session.LogicSettings.UseWalkingSpeedVariant && walkSpeed == 0)
                {
                    CurrentWalkingSpeed = session.Navigation.VariantRandom(session, CurrentWalkingSpeed);
                    speedInMetersPerSecond = CurrentWalkingSpeed / 3.6;
                }

                nextWaypointDistance = Math.Min(currentDistanceToTarget,
                    millisecondsUntilGetUpdatePlayerLocationResponse / 1000 * speedInMetersPerSecond);
                nextWaypointBearing = LocationUtils.DegreeBearing(sourceLocation, destinaionCoordinate);
                waypoint = await LocationUtils.CreateWaypoint(sourceLocation, nextWaypointDistance, nextWaypointBearing).ConfigureAwait(false);

                requestSendDateTime = DateTime.Now;
                await LocationUtils.UpdatePlayerLocationWithAltitude(session, waypoint, (float)speedInMetersPerSecond).ConfigureAwait(false);

                base.DoUpdatePositionEvent(session, waypoint.Latitude, waypoint.Longitude, CurrentWalkingSpeed);

                if (functionExecutedWhileWalking != null)
                    await functionExecutedWhileWalking().ConfigureAwait(false); // look for pokemon & hit stops
            } while (LocationUtils.CalculateDistanceInMeters(sourceLocation, destinaionCoordinate) >= 2);
        }

        public override double CalculateDistance(Coordinate source, Coordinate destination)
        {
            return LocationUtils.CalculateDistanceInMeters(source.Latitude, source.Longitude, destination.Latitude, destination.Longitude);
        }
    }
}
*/