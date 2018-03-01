/*
Copyright (c) 2018 Travis J Martin (travis.martin) [at} isogrid.org)

This file is part of IsoSwitch.201802

IsoSwitch.201802 is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 3 as published
by the Free Software Foundation.

IsoSwitch.201802 is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License version 3 for more details.

You should have received a copy of the GNU General Public License version 3
along with IsoSwitch.201802.  If not, see <http://www.gnu.org/licenses/>.

A) We, the undersigned contributors to this file, declare that our
   contribution was created by us as individuals, on our own time, entirely for
   altruistic reasons, with the expectation and desire that the Copyright for our
   contribution would expire in the year 2038 and enter the public domain.
B) At the time when you first read this declaration, you are hereby granted a license
   to use this file under the terms of the GNU General Public License, v3.
C) Additionally, for all uses of this file after Jan 1st 2038, we hereby waive
   all copyright and related or neighboring rights together with all associated claims
   and causes of action with respect to this work to the extent possible under law.
D) We have read and understand the terms and intended legal effect of CC0, and hereby
   voluntarily elect to apply it to this file for all uses or copies that occur
   after Jan 1st 2038.
E) To the extent that this file embodies any of our patentable inventions, we
   hearby grant you a worldwide, royalty-free, non-exclusive, perpetual license to
   those inventions.

|      Signature       |  Declarations   |                                                     Acknowledgments                                                       |
|:--------------------:|:---------------:|:-------------------------------------------------------------------------------------------------------------------------:|
|   Travis J Martin    |    A,B,C,D,E    | My loving wife, Lindsey Ann Irwin Martin, for her incredible support on our journey!                                      |

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IsoSwitchLib
{
  public class GpsTime
  {
    // This corrects for Leap Seconds. If a Leap Second is announced,
    // this MUST be updated to the new offset and re-released. A new
    // or reset IsoSwitch can sync to the network if the time is off
    // by 1 second, but not if it is off by 2 seconds.
    private const int GpsTimeUtcOffset = 18;

    public const UInt64 TwoTicks = 2UL << 30;
    public const UInt64 OneTick = 1 << 30;

    public const UInt64 OneSecond = 1 << 27;

    public const UInt64 OneFrame = OneSecond >> 10;

    // The point in time when GPS Time began, adding in Leap Seconds
    // GPS Time began at 00:00:00 UTC (00:00:19 TAI) on January 6, 1980
    // Extend the epoch into the past by the number of leap seconds.
    // This means a TimeSpan between GpsEpochStart and an accurate UTC DateTime
    // reflects accurate GPS Time.
    private static DateTime GpsEpochStart = (new DateTime(1980, 1, 6)).Subtract(new TimeSpan(0, 0, GpsTimeUtcOffset));
    
    internal GpsTime()
    {
    }

    public enum GpsTimeKind
    {
      Unknown,
      Implicit,
      Explicit,
    };

    // Convert a GpsTime in (37.27) format to a 2-bit Tick
    public static TickMod ToTick(UInt64 gpsTime) => (TickMod)((UInt32)(gpsTime) >> 30);

    // This function converts a TimeSpan in 100-nanosecond units to 
    // GPS Time in seconds formatted fixed point (37.27)
    public static UInt64 GpsTime37_27FromTimeSpan(TimeSpan timeSpan)
    {
      UInt64 gpsTime100Nanoseconds_0 = (UInt64)timeSpan.Ticks;

      // Convert to 10-microsecond units, fixed point 7 bits of fraction
      UInt64 gpsTime10Microseconds_0 = gpsTime100Nanoseconds_0 / 100;
      UInt64 gpsTime10Microseconds_7 = gpsTime10Microseconds_0 << 7;

      // Convert to millisecond units, fixed point 13 bits of fraction
      UInt64 gpsTimeMilliseconds_7 = gpsTime10Microseconds_7 / 100;
      UInt64 gpsTimeMilliseconds_13 = gpsTimeMilliseconds_7 << 6;

      // Convert to 100-millisecond units, fixed point 20 bits of fraction
      UInt64 gpsTime100Milliseconds_13 = gpsTimeMilliseconds_13 / 100;
      UInt64 gpsTime100Milliseconds_20 = gpsTime100Milliseconds_13 << 7;

      // Convert to 1-second units, fixed point 27 bits of fraction
      UInt64 gpsTimeSeconds_20 = gpsTime100Milliseconds_20 / 10;
      UInt64 gpsTimeSeconds_27 = gpsTimeSeconds_20 << 7;

      return gpsTimeSeconds_27;
    }

    // This function converts a DateTime in UTC to GPS Time in seconds
    // formatted fixed point (37.27)
    public static UInt64 GpsTime37_27FromUtcDateTime(DateTime time) =>
      GpsTime37_27FromTimeSpan(time.Subtract(GpsEpochStart));

    // This function converts a Timestamp in Stopwatch units to 
    // GPS Time in seconds formatted fixed point (37.27)
    public static UInt64 GpsTime37_27FromTimeStamp(long timeStamp)
    {
      // Convert to fixed point 10 bits of fraction
      UInt64 gpsTimeFreq_0 = (UInt64)timeStamp;
      UInt64 gpsTimeFreq_10 = gpsTimeFreq_0 << 10;

      if (gpsTimeFreq_10 >> 10 != gpsTimeFreq_0) throw new ArgumentOutOfRangeException("timeStamp too large");

      // Convert to 1-second units fixed point 27 bits of fraction
      UInt64 gpsTimeSeconds_10 = gpsTimeFreq_10 / (UInt64)Stopwatch.Frequency;
      UInt64 gpsTimeSeconds_27 = gpsTimeSeconds_10 << 17;

      return gpsTimeSeconds_27 / IsoBridge.SpeedFactor;
    }

    private UInt64 _lastPingGpsTime = 0;

    public void ResetPingTimes()
    {
      _receivedPingGpsTimes.Initialize();
      _lastPingGpsTime = 0;
      _minPingGpsTime = UInt64.MaxValue;
      _maxPingGpsTime = 0;
      _gpsTime = 0;
    }

    public void HandlePing(UInt64 route, UInt64 gpsTime)
    {
      if (Kind != GpsTimeKind.Unknown)
      {
        // Once the GpsTime has been set (either Explicit or Implicit),
        // there's no need for GpsTime to handle pings.
        return;
      }

      if (gpsTime == 0)
      {
        // Just an initial ping that contains route info, ignore for now
        return;
      }

      UInt64 gpstime = _gpsTime;
      UInt64 gpsTimeSinceLastPing = gpstime - _lastPingGpsTime;
      lock (this)
      {

        for (UInt64 i = 0; i < 4; i++)
        {
          if (i == route)
          {
            _receivedPingGpsTimes[i] = gpsTime;
          }
          else if (_receivedPingGpsTimes[i] != 0)
          {
            _receivedPingGpsTimes[i] += gpsTimeSinceLastPing;
          }
          else
          {
            continue;
          }

          if (_receivedPingGpsTimes[i] < MinPingGpsTime)
          {
            MinPingGpsTime = _receivedPingGpsTimes[i];
          }

          if (_receivedPingGpsTimes[i] > MaxPingGpsTime)
          {
            MaxPingGpsTime = _receivedPingGpsTimes[i];
          }
        }

        _lastPingGpsTime = gpstime;
      }
    }

    public TickMod TwoTicksAgo => ToTick(Now - GpsTime.TwoTicks);
    public TickMod OneTickAgo => ToTick(Now - GpsTime.OneTick);
    public TickMod CurrentTick => ToTick(Now);
    public TickMod NextTick => ToTick(Now + GpsTime.OneTick);

    public UInt64 Now => _gpsTime;

    private UInt64[] _receivedPingGpsTimes = new UInt64[4];

    private UInt64 _minPingGpsTime = UInt64.MaxValue;
    public UInt64 MinPingGpsTime
    {
      get { return _minPingGpsTime; }
      private set { _minPingGpsTime = value; }
    }

    private UInt64 _maxPingGpsTime = 0;
    public UInt64 MaxPingGpsTime
    {
      get { return _maxPingGpsTime; }
      private set { _maxPingGpsTime = value; }
    }

    // Coherency is defined as less than a 2 second agreement between all received times
    public bool IsCoherent => (MaxPingGpsTime != 0) && (MaxPingGpsTime - MinPingGpsTime) < 0x10000000;

    public GpsTimeKind Kind { get; private set; }

    public bool SetImplicitTimeIfCoherent()
    {
      lock (this)
      {
        if (IsCoherent)
        {
          // Calculate the average of received PingGpsTimes
          UInt64 total = 0;
          UInt64 count = 0;
          foreach (UInt64 gpsTime in _receivedPingGpsTimes)
          {
            if (gpsTime != 0)
            {
              total += gpsTime;
              count++;
            }
          }

          _gpsTime = total / count;

          if (Kind != GpsTimeKind.Explicit)
          {
            Kind = GpsTimeKind.Implicit;
          }

          return true;
        }
      }

      return false;
    }

    private UInt64 _gpsTime;

    public void SetExplicitTime(DateTime utcDateTime) => SetExplicitGpsTime(GpsTime37_27FromUtcDateTime(utcDateTime));

    public void SetExplicitGpsTime(UInt64 gpsTime)
    {
      // Do this in a loop to avoid having to synchronize
      while (_gpsTime != gpsTime)
      {
        _gpsTime = gpsTime;
        Kind = GpsTimeKind.Explicit;
      }
    }

    // Increment the current time by 1 / (2 ^ 10) seconds
    internal void Increment_10()
    {
      _gpsTime += 1 << (27 - 10);

      if ((_gpsTime & 0x3FFFFFFF) == 0)
      {
        Console.WriteLine("GpsTimeTick");
      }
    }

    // TEST METHOD: Increment the current time by OneSecond
    internal void Test_IncrementSecond() => _gpsTime += OneSecond;

    // TEST METHOD: Increment the current time by OneTick
    internal void Test_IncrementTick() => _gpsTime += OneTick;
  }
}
