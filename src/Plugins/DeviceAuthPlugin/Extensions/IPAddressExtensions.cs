namespace DeviceAuthPlugin.Extensions
{
    using System.Net;

    // Credits: https://stackoverflow.com/a/2138724
    public static class IPAddressExtensions
    {
        public static bool IsInRange(this string ipAddress, string start, string end)
        {
            var ipAddr = IPAddress.Parse(ipAddress);
            var startAddr = IPAddress.Parse(start);
            var endAddr = IPAddress.Parse(end);
            var result = IsInRange(ipAddr, startAddr, endAddr);
            return result;
        }

        public static bool IsInRange(IPAddress ipAddress, IPAddress start, IPAddress end)
        {
            if (ipAddress.AddressFamily != start.AddressFamily)
            {
                return false;
            }

            var addressBytes = ipAddress.GetAddressBytes();
            var lowerBytes = start.GetAddressBytes();
            var upperBytes = end.GetAddressBytes();
            var lowerBoundary = true;
            var upperBoundary = true;
            for (var i = 0; i < lowerBytes.Length && (lowerBoundary || upperBoundary); i++)
            {
                if ((lowerBoundary && addressBytes[i] < lowerBytes[i]) ||
                    (upperBoundary && addressBytes[i] > upperBytes[i]))
                {
                    return false;
                }

                lowerBoundary &= (addressBytes[i] == lowerBytes[i]);
                upperBoundary &= (addressBytes[i] == upperBytes[i]);
            }

            return true;
        }
    }
}
