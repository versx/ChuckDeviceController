/*
namespace ChuckDeviceConfigurator.Services.Routing.Walking
{
    using ChuckDeviceController.Geometry.Models;

    internal abstract class BaseWalkStrategy : IWalkStrategy
    {
        protected double _currentWalkingSpeed = 0;
        protected const double SpeedDownTo = 10 / 3.6;
        protected double _minStepLengthInMeters = 1.3d;
        protected bool isCancelled = false;
        protected readonly Random _randWalking = new Random();
        public event UpdatePositionDelegate UpdatePositionEvent;
        public virtual List<Coordinate> Points { get; set; }
        public abstract Task Walk(Coordinate targetLocation, Func<Task> functionExecutedWhileWalking, CancellationToken cancellationToken, double walkSpeed = 0.0);
        public virtual string RouteName { get; }
        public BaseWalkStrategy()
        {
        }
        public void OnStartWalking(Coordinate desination, double calculatedDistance = 0.0)
        {
            var distance = calculatedDistance;
            if (distance == 0)
            {
                distance = CalculateDistance(session.Client.CurrentLatitude, session.Client.CurrentLongitude,
                    desination.Latitude, desination.Longitude);
            }
            if (desination is FortLocation)
            {
                var fortLocation = desination as FortLocation;
                session.EventDispatcher.Send(new FortTargetEvent
                {
                    Name = desination.Name,
                    Distance = distance,
                    Route = RouteName,
                    Type = fortLocation.FortData.Type
                });
            }
        }
        internal void DoUpdatePositionEvent(double latitude, double longitude, double speed, double variant = 0.0)
        {
            UpdatePositionEvent?.Invoke(session, latitude, longitude, speed);
        }
        /// <summary>
        /// Cell phones Gps systems can't generate accurate GEO, the average best they can is 5 meter.
        /// http://gis.stackexchange.com/questions/43617/what-is-the-maximum-theoretical-accuracy-of-gps
        /// </summary>
        public async Task<Coordinate> GenerateUnaccurateGeocoordinate(Coordinate geo, double nextWaypointBearing)
        {
            var minBearing = Convert.ToInt32(nextWaypointBearing - 40);
            minBearing = minBearing > 0 ? minBearing : minBearing * -1;
            var maxBearing = Convert.ToInt32(nextWaypointBearing + 40);
            maxBearing = maxBearing < 360 ? maxBearing : 360 - maxBearing;
            var randomBearingDegrees = _randWalking.NextDouble() +
                                       _randWalking.Next(
                                           Math.Min(minBearing, maxBearing),
                                           Math.Max(minBearing, maxBearing)
                                       );
            var randomDistance = _randWalking.NextDouble() * 3;
            return await LocationUtils.CreateWaypoint(geo, randomDistance, randomBearingDegrees).ConfigureAwait(false);
        }
        public Task RedirectToNextFallbackStrategy(ILogicSettings logicSettings,
            Coordinate targetLocation, Func<Task> functionExecutedWhileWalking,
            CancellationToken cancellationToken, double walkSpeed = 0.0)
        {
            var nextStrategy = session.Navigation.GetStrategy(logicSettings);
            return nextStrategy.Walk(targetLocation, functionExecutedWhileWalking, session, cancellationToken);
        }
        public async Task DoWalk(List<Coordinate> points, Func<Task> functionExecutedWhileWalking,
            Coordinate sourceLocation, Coordinate targetLocation,
            CancellationToken cancellationToken, double walkSpeed = 0.0)
        {
            var currentLocation = new Coordinate(_client.CurrentLatitude, _client.CurrentLongitude);
            //filter google defined waypoints and remove those that are too near to the previous ones
            var waypointsDists = new Dictionary<Tuple<Coordinate, Coordinate>, double>();
            var minWaypointsDistance = RandomizeStepLength(_minStepLengthInMeters);
            for (var i = 0; i < points.Count; i++)
            {
                if (i > 0)
                {
                    var dist = LocationUtils.CalculateDistanceInMeters(points[i - 1], points[i]);
                    waypointsDists[new Tuple<Coordinate, Coordinate>(points[i - 1], points[i])] = dist;
                }
            }
            var tooNearPoints = waypointsDists.Where(kvp => kvp.Value < minWaypointsDistance)
                .Select(kvp => kvp.Key.Item1)
                .ToList();
            foreach (var tooNearPoint in tooNearPoints)
            {
                points.Remove(tooNearPoint);
            }
            if (points.Any()) //check if first waypoint is the current location (this is what google returns), in such case remove it!
            {
                var firstStep = points.First();
                if (firstStep == currentLocation)
                    points.Remove(points.First());
            }
            Points = points;
            var walkedPointsList = new List<Coordinate>();
            foreach (var nextStep in points)
            {
                currentLocation = new Coordinate(_client.CurrentLatitude, _client.CurrentLongitude);
                if (_currentWalkingSpeed <= 0)
                    _currentWalkingSpeed = session.LogicSettings.WalkingSpeedInKilometerPerHour;
                if (session.LogicSettings.UseWalkingSpeedVariant && walkSpeed == 0)
                    _currentWalkingSpeed = session.Navigation.VariantRandom(session, _currentWalkingSpeed);
                var speedInMetersPerSecond = (walkSpeed > 0 ? walkSpeed : _currentWalkingSpeed) / 3.6;
                var nextStepBearing = LocationUtils.DegreeBearing(currentLocation, nextStep);
                //particular steps are limited by minimal length, first step is calculated from the original speed per second (distance in 1s)
                var nextStepDistance = Math.Max(RandomizeStepLength(_minStepLengthInMeters), speedInMetersPerSecond);
                var waypoint = await LocationUtils.CreateWaypoint(currentLocation, nextStepDistance, nextStepBearing).ConfigureAwait(false);
                walkedPointsList.Add(waypoint);
                var previousLocation =
                    currentLocation; //store the current location for comparison and correction purposes
                var requestSendDateTime = DateTime.Now;
                await LocationUtils.UpdatePlayerLocationWithAltitude(session, waypoint,
                        (float)speedInMetersPerSecond).ConfigureAwait(false);
                var realDistanceToTarget = LocationUtils.CalculateDistanceInMeters(currentLocation, targetLocation);
                if (realDistanceToTarget < 2)
                    break;
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var msToPositionChange = (DateTime.Now - requestSendDateTime).TotalMilliseconds;
                    currentLocation = new Coordinate(_client.CurrentLatitude, _client.CurrentLongitude);
                    var currentDistanceToWaypoint = LocationUtils.CalculateDistanceInMeters(currentLocation, nextStep);
                    realDistanceToTarget = LocationUtils.CalculateDistanceInMeters(currentLocation, targetLocation);
                    var realSpeedinMperS = nextStepDistance / (msToPositionChange / 1000);
                    var realDistanceWalked = LocationUtils.CalculateDistanceInMeters(previousLocation, currentLocation);
                    //if the real calculated speed is lower than the one expected, we will raise the speed for the following step
                    double speedRaise = 0;
                    if (realSpeedinMperS < speedInMetersPerSecond)
                        speedRaise = speedInMetersPerSecond - realSpeedinMperS;
                    double distanceRaise = 0;
                    if (realDistanceWalked < nextStepDistance)
                        distanceRaise = nextStepDistance - realDistanceWalked;
                    var realDistanceToTargetSpeedDown =
                        LocationUtils.CalculateDistanceInMeters(currentLocation, targetLocation);
                    if (realDistanceToTargetSpeedDown < 40)
                        if (speedInMetersPerSecond > SpeedDownTo)
                            speedInMetersPerSecond = SpeedDownTo;
                    if (session.LogicSettings.UseWalkingSpeedVariant && walkSpeed == 0)
                    {
                        _currentWalkingSpeed = session.Navigation.VariantRandom(session, _currentWalkingSpeed);
                        speedInMetersPerSecond = _currentWalkingSpeed / 3.6;
                    }
                    speedInMetersPerSecond += speedRaise;
                    if (walkSpeed > 0)
                    {
                        speedInMetersPerSecond = walkSpeed / 3.6;
                    }
                    nextStepBearing = LocationUtils.DegreeBearing(currentLocation, nextStep);
                    //setting next step distance is limited by the target and the next waypoint distance (we don't want to miss them)
                    //also the minimal step length is used as we don't want to spend minutes jumping by cm lengths
                    nextStepDistance = Math.Min(Math.Min(realDistanceToTarget, currentDistanceToWaypoint),
                        //also add the distance raise (bot overhead corrections) to the normal step length
                        Math.Max(RandomizeStepLength(_minStepLengthInMeters) + distanceRaise,
                            (msToPositionChange / 1000) * speedInMetersPerSecond) + distanceRaise);
                    int timeToWalk = (int)((nextStepDistance * 1000) / speedInMetersPerSecond);
                    //Logger.Debug($"nextStepDistance {nextStepDistance} need {timeToWalk} ms");
                    waypoint = await LocationUtils.CreateWaypoint(currentLocation, nextStepDistance, nextStepBearing).ConfigureAwait(false);
                    walkedPointsList.Add(waypoint);
                    //store the current location for comparison and correction purposes
                    previousLocation = currentLocation;
                    requestSendDateTime = DateTime.Now;
                    await LocationUtils.UpdatePlayerLocationWithAltitude(session, waypoint, (float)speedInMetersPerSecond).ConfigureAwait(false);
                    UpdatePositionEvent?.Invoke(session, waypoint.Latitude, waypoint.Longitude, _currentWalkingSpeed);
                    await Task.Delay(timeToWalk).ConfigureAwait(false);
                    if (functionExecutedWhileWalking != null)
                        await functionExecutedWhileWalking().ConfigureAwait(false); // look for pokemon
                } while (LocationUtils.CalculateDistanceInMeters(currentLocation, nextStep) >= 2);
                UpdatePositionEvent?.Invoke(session, nextStep.Latitude, nextStep.Longitude, _currentWalkingSpeed);
            }
        }
        /// <summary>
        /// Basic step length is given but we want to randomize it a bit to avoid usage of steps of the same length
        /// </summary>
        /// <param name="initialStepLength">Length of the step in meters</param>
        /// <returns></returns>
        protected double RandomizeStepLength(double initialStepLength)
        {
            var randFactor = 0.3d;
            var initialStepLengthMm = initialStepLength * 1000;
            var randomMin = (int)(initialStepLengthMm * (1 - randFactor));
            var randomMax = (int)(initialStepLengthMm * (1 + randFactor));
            var randStep = _randWalking.Next(randomMin, randomMax);
            return randStep / 1000d;
        }
        public virtual double CalculateDistance(Coordinate source, Coordinate destination)
        {
            return LocationUtils.CalculateDistanceInMeters(source.Latitude, source.Longitude, destination.Latitude, destination.Longitude);
        }
    }
}
*/