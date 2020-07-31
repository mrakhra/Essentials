using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Xamarin.Essentials
{
    public static partial class Connectivity
    {
        static event EventHandler<ConnectivityChangedEventArgs> ConnectivityChangedInternal;

        // a cache so that events aren't fired unnecessarily
        // this is mainly an issue on Android, but we can stiil do this everywhere
        static NetworkAccess currentAccess;
        static List<ConnectionProfile> currentProfiles;

        public static NetworkAccess NetworkAccess => PlatformNetworkAccess;

        public static IEnumerable<ConnectionProfile> ConnectionProfiles => PlatformConnectionProfiles.Distinct();

        public static event EventHandler<ConnectivityChangedEventArgs> ConnectivityChanged
        {
            add
            {
                var wasRunning = ConnectivityChangedInternal != null;

                ConnectivityChangedInternal += value;

                if (!wasRunning && ConnectivityChangedInternal != null)
                {
                    SetCurrent();
                    StartListeners();
                }
            }

            remove
            {
                var wasRunning = ConnectivityChangedInternal != null;

                ConnectivityChangedInternal -= value;

                if (wasRunning && ConnectivityChangedInternal == null)
                    StopListeners();
            }
        }

        static void SetCurrent()
        {
            try
            {
                currentAccess = NetworkAccess;
                currentProfiles = new List<ConnectionProfile>(ConnectionProfiles);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        static void OnConnectivityChanged(NetworkAccess access, IEnumerable<ConnectionProfile> profiles)
            => OnConnectivityChanged(new ConnectivityChangedEventArgs(access, profiles));

        static void OnConnectivityChanged()
            => OnConnectivityChanged(NetworkAccess, ConnectionProfiles);

        static void OnConnectivityChanged(ConnectivityChangedEventArgs e)
        {
            try
            {
                if (currentAccess == e.NetworkAccess && currentProfiles.SequenceEqual(e.ConnectionProfiles))
                    return;

                SetCurrent();
                MainThread.BeginInvokeOnMainThread(() => ConnectivityChangedInternal?.Invoke(null, e));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }

    public class ConnectivityChangedEventArgs : EventArgs
    {
        public ConnectivityChangedEventArgs(NetworkAccess access, IEnumerable<ConnectionProfile> connectionProfiles)
        {
            NetworkAccess = access;
            ConnectionProfiles = connectionProfiles;
        }

        public NetworkAccess NetworkAccess { get; }

        public IEnumerable<ConnectionProfile> ConnectionProfiles { get; }

        public override string ToString() =>
            $"{nameof(NetworkAccess)}: {NetworkAccess}, " +
            $"{nameof(ConnectionProfiles)}: [{string.Join(", ", ConnectionProfiles)}]";
    }
}
