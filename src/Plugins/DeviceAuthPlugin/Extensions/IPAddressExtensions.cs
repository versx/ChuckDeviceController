namespace DeviceAuthPlugin.Extensions;

using System.Collections;
using System.Net;
using System.Net.Sockets;

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

    // Credits: https://stackoverflow.com/a/2138724
    public static bool IsInRange(this IPAddress ipAddress, IPAddress start, IPAddress end)
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

    public static bool IsInSubnet(this string ipAddress, string subnetMask)
    {
        var ipAddr = IPAddress.Parse(ipAddress);
        var result = IsInSubnet(ipAddr, subnetMask);
        return result;
    }

    // Credits: https://stackoverflow.com/a/56461160
    public static bool IsInSubnet(this IPAddress ipAddress, string subnetMask)
    {
        var slashIdx = subnetMask.IndexOf('/');
        if (slashIdx == -1)
        { // We only handle netmasks in format "IP/PrefixLength".
            throw new NotSupportedException("Only SubNetMasks with a given prefix length are supported.");
        }

        // First parse the address of the netmask before the prefix length.
        var maskAddress = IPAddress.Parse(subnetMask[..slashIdx]);

        if (maskAddress.AddressFamily != ipAddress.AddressFamily)
        { // We got something like an IPV4-Address for an IPv6-Mask. This is not valid.
            return false;
        }

        // Now find out how long the prefix is.
        var maskLength = int.Parse(subnetMask[(slashIdx + 1)..]);
        if (maskLength == 0)
        {
            return true;
        }

        if (maskLength < 0)
        {
            throw new NotSupportedException("A Subnetmask should not be less than 0.");
        }

        if (maskAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            // Convert the mask address to an unsigned integer.
            var maskAddressBits = BitConverter.ToUInt32(maskAddress.GetAddressBytes().Reverse().ToArray(), 0);

            // And convert the IpAddress to an unsigned integer.
            var ipAddressBits = BitConverter.ToUInt32(ipAddress.GetAddressBytes().Reverse().ToArray(), 0);

            // Get the mask/network address as unsigned integer.
            var mask = uint.MaxValue << (32 - maskLength);

            // https://stackoverflow.com/a/1499284/3085985
            // Bitwise AND mask and MaskAddress, this should be the same as mask and IpAddress
            // as the end of the mask is 0000 which leads to both addresses to end with 0000
            // and to start with the prefix.
            return (maskAddressBits & mask) == (ipAddressBits & mask);
        }

        if (maskAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // Convert the mask address to a BitArray. Reverse the BitArray to compare the bits of each byte in the right order.
            var maskAddressBits = new BitArray(maskAddress.GetAddressBytes().Reverse().ToArray());

            // And convert the IpAddress to a BitArray. Reverse the BitArray to compare the bits of each byte in the right order.
            var ipAddressBits = new BitArray(ipAddress.GetAddressBytes().Reverse().ToArray());
            var ipAddressLength = ipAddressBits.Length;

            if (maskAddressBits.Length != ipAddressBits.Length)
            {
                throw new ArgumentException("Length of IP Address and Subnet Mask do not match.");
            }

            // Compare the prefix bits.
            for (var i = ipAddressLength - 1; i >= ipAddressLength - maskLength; i--)
            {
                if (ipAddressBits[i] != maskAddressBits[i])
                {
                    return false;
                }
            }

            return true;
        }

        throw new NotSupportedException("Only InterNetworkV6 or InterNetwork address families are supported.");
    }
}