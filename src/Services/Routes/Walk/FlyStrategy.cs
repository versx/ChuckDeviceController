/*
namespace ChuckDeviceController.Services.Routes.Walk
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using ChuckDeviceController.Data.Entities;

    internal class FlyStrategy : BaseWalkStrategy
    {
        public FlyStrategy() : base()
        {
        }

        public override string RouteName => "NecroBot Flying";

        public override async Task Walk(Coordinate targetLocation, Func<Task> functionExecutedWhileWalking, CancellationToken cancellationToken, double walkSpeed = 0.0)
        {
            var curLocation = new Coordinate(_client.CurrentLatitude, _client.CurrentLongitude);
            var destinaionCoordinate = new Coordinate(targetLocation.Latitude, targetLocation.Longitude);

            var dist = LocationUtils.CalculateDistanceInMeters(curLocation, destinaionCoordinate);
            if (dist >= 100)
            {
                var nextWaypointDistance = dist * 70 / 100;
                var nextWaypointBearing = LocationUtils.DegreeBearing(curLocation, destinaionCoordinate);

                var waypoint = await LocationUtils.CreateWaypoint(curLocation, nextWaypointDistance, nextWaypointBearing).ConfigureAwait(false);
                var sentTime = DateTime.Now;

                // We are setting speed to 0, so it will be randomly generated speed.
                await LocationUtils.UpdatePlayerLocationWithAltitude(session, waypoint, 0).ConfigureAwait(false);
                base.DoUpdatePositionEvent(session, waypoint.Latitude, waypoint.Longitude, walkSpeed, 0);

                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var millisecondsUntilGetUpdatePlayerLocationResponse =
                        (DateTime.Now - sentTime).TotalMilliseconds;

                    curLocation = new Coordinate(_client.CurrentLatitude, _client.CurrentLongitude);
                    var currentDistanceToTarget = LocationUtils.CalculateDistanceInMeters(curLocation, destinaionCoordinate);

                    dist = LocationUtils.CalculateDistanceInMeters(curLocation, destinaionCoordinate);

                    if (dist >= 100)
                        nextWaypointDistance = dist * 70 / 100;
                    else
                        nextWaypointDistance = dist;

                    nextWaypointBearing = LocationUtils.DegreeBearing(curLocation, destinaionCoordinate);
                    waypoint = await LocationUtils.CreateWaypoint(curLocation, nextWaypointDistance, nextWaypointBearing).ConfigureAwait(false);
                    sentTime = DateTime.Now;
                    // We are setting speed to 0, so it will be randomly generated speed.
                    await LocationUtils.UpdatePlayerLocationWithAltitude(session, waypoint, 0).ConfigureAwait(false);
                    base.DoUpdatePositionEvent(session, waypoint.Latitude, waypoint.Longitude, walkSpeed);


                    if (functionExecutedWhileWalking != null)
                        await functionExecutedWhileWalking().ConfigureAwait(false); // look for pokemon
                } while (LocationUtils.CalculateDistanceInMeters(curLocation, destinaionCoordinate) >= 10);
            }
            else
            {
                // We are setting speed to 0, so it will be randomly generated speed.
                await LocationUtils.UpdatePlayerLocationWithAltitude(session, targetLocation.ToGeoCoordinate(), 0).ConfigureAwait(false);
                base.DoUpdatePositionEvent(session, targetLocation.Latitude, targetLocation.Longitude, walkSpeed);
                if (functionExecutedWhileWalking != null)
                    await functionExecutedWhileWalking().ConfigureAwait(false); // look for pokemon
            }
        }

        public override double CalculateDistance(Coordinate source, Coordinate destination)
        {
            return LocationUtils.CalculateDistanceInMeters(source.Latitude, source.Longitude, destination.Latitude, destination.Longitude);
        }
    }
}
*/