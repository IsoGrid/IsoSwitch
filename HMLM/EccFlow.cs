/*
Copyright (c) 2018 Travis J Martin (travis.martin) [at} isogrid.org)

This file is part of IsoSwitch.201801

IsoSwitch.201801 is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 3 as published
by the Free Software Foundation.

IsoSwitch.201801 is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License version 3 for more details.

You should have received a copy of the GNU General Public License version 3
along with IsoSwitch.201801.  If not, see <http://www.gnu.org/licenses/>.

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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMLM
{
    public class EccFlowConstraints
    {
        // Specifies the reliability requirement of the EccFlow
        // 0 ==> Expect < 1 failure every second
        // 1 ==> Expect < 1 failure every 2 seconds
        // 2 ==> Expect < 1 failure every 4 seconds
        // N ==> Expect < 1 failure every 2^N seconds
        private int minReliability;
        public int MinReliability
        {
            get { return minReliability; }
            set
            {
                if (value <= 40)
                {
                    minReliability = value;
                }
                else
                {
                    // 2^40 seconds is a long time
                    minReliability = 40;
                }
            }
        }

        // Specifies the end-to-end latency requirement of the EccFlow in microseconds
        private int maxLatency;
        public int MaxLatencyUs
        {
            get { return maxLatency; }
            set { maxLatency = value; }
        }

        // Specifies the data throughput required by the EccFlow
        private int minThroughputBytesPerSecond;
        public int MinThroughputBytesPerSecond
        {
            get { return minThroughputBytesPerSecond; }
            set { minThroughputBytesPerSecond = value; }
        }

        // Specifies the credit throughput required by the EccFlow
        private int minThroughputCreditsPerSecond;
        public int MinThroughputCreditsPerSecond
        {
            get { return minThroughputCreditsPerSecond; }
            set { minThroughputCreditsPerSecond = value; }
        }

        // Specifies the maximum credit cost per second allowed by the EccFlow
        private int maxCreditCostPerSecond;
        public int MaxCreditCostPerSecond
        {
            get { return maxCreditCostPerSecond; }
            set { maxCreditCostPerSecond = value; }
        }

        // Specifies the maximum credit cost per byte allowed by the EccFlow
        private int maxCreditCostPerByte;
        public int MaxCreditCostPerByte
        {
            get { return maxCreditCostPerByte; }
            set { maxCreditCostPerByte = value; }
        }
    }

    public struct LOCATORCOMP : IComparable<LOCATORCOMP>
    {
        public LOCATORCOMP(LOCATORHASH first, LOCATORHASH second)
        {
            Id0 = first.Id0 ^ second.Id0;
        }

        public readonly long Id0;

        public int CompareTo(LOCATORCOMP other)
        {
            if ((ulong)this.Id0 < (ulong)other.Id0)
            {
                return -1;
            }

            if ((ulong)this.Id0 > (ulong)other.Id0)
            {
                return 1;
            }

            return 0;
        }

        public static bool operator <(LOCATORCOMP op1, LOCATORCOMP op2) => op1.CompareTo(op2) == -1;
        public static bool operator >(LOCATORCOMP op1, LOCATORCOMP op2) => op1.CompareTo(op2) == 1;
        public static bool operator <=(LOCATORCOMP op1, LOCATORCOMP op2) => op1.CompareTo(op2) != 1;
        public static bool operator >=(LOCATORCOMP op1, LOCATORCOMP op2) => op1.CompareTo(op2) != -1;
        public static bool operator ==(LOCATORCOMP op1, LOCATORCOMP op2) => op1.CompareTo(op2) == 0;
        public static bool operator !=(LOCATORCOMP op1, LOCATORCOMP op2) => op1.CompareTo(op2) != 0;
        public override bool Equals(Object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            
            return (this == (LOCATORCOMP)obj);
        }
        public override int GetHashCode()
        {
            return Id0.GetHashCode();
        }
    }

    public struct LOCATORHASH : IComparable<LOCATORHASH>
    {
        private LOCATORHASH(long id0)
        {
            Id0 = id0;
        }
        
        public readonly long Id0;

        public LOCATORHASH GetSuffix(int prefixBits)
        {
            return SuffixMask(prefixBits) & this;
        }

        public LOCATORHASH GetPrefix(int prefixBits)
        {
            if (prefixBits == 0)
            {
                return LOCATORHASH.InitZero();
            }
            else
            {
                return PrefixMask(prefixBits) & this;
            }
        }

        private LOCATORHASH PrefixMask(int prefixBits)
        {
            return new LOCATORHASH(((long)-1) << (64 - prefixBits));
        }

        private LOCATORHASH SuffixMask(int prefixBits)
        {
            return new LOCATORHASH((long)(unchecked((ulong)-1) >> prefixBits));
        }

        public byte this[int i]
        {
            get
            {
                int x = 7 - (i % 8);
                switch (i / 8)
                {
                    case 0: return (byte)(Id0 >> (x * 8));

                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public bool GetBit(int i)
        {
            return ((GetLong(i / 64) >> (63 - (i % 64))) & 1) == 1;
        }

        public long GetLong(int i)
        {
            return Id0;
        }

        public int CompareTo(LOCATORHASH other)
        {
            if ((ulong)this.Id0 < (ulong)other.Id0)
            {
                return -1;
            }

            if ((ulong)this.Id0 > (ulong)other.Id0)
            {
                return 1;
            }

            return 0;
        }

        public static bool operator <(LOCATORHASH op1, LOCATORHASH op2) => op1.CompareTo(op2) == -1;
        public static bool operator >(LOCATORHASH op1, LOCATORHASH op2) => op1.CompareTo(op2) == 1;
        public static bool operator <=(LOCATORHASH op1, LOCATORHASH op2) => op1.CompareTo(op2) != 1;
        public static bool operator >=(LOCATORHASH op1, LOCATORHASH op2) => op1.CompareTo(op2) != -1;
        public static bool operator ==(LOCATORHASH op1, LOCATORHASH op2) => op1.CompareTo(op2) == 0;
        public static bool operator !=(LOCATORHASH op1, LOCATORHASH op2) => op1.CompareTo(op2) != 0;
        public override bool Equals(Object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return (this == (LOCATORHASH)obj);
        }
        public override int GetHashCode()
        {
            return Id0.GetHashCode();
        }

        public static LOCATORHASH operator |(LOCATORHASH op1, LOCATORHASH op2)
        {
            return new LOCATORHASH(op1.Id0 | op2.Id0);
        }

        public static LOCATORHASH operator ^(LOCATORHASH op1, LOCATORHASH op2)
        {
            return new LOCATORHASH(op1.Id0 ^ op2.Id0);
        }

        public static LOCATORHASH operator &(LOCATORHASH op1, LOCATORHASH op2)
        {
            return new LOCATORHASH(op1.Id0 & op2.Id0);
        }

        public static LOCATORHASH InitBit(int i)
        {
            if (i > 63)
                throw new IndexOutOfRangeException();

            return new LOCATORHASH(Convert.ToInt64(1) << (63 - i));
        }

        public static LOCATORHASH InitByte(int i, byte val)
        {
            if (i > 7) throw new IndexOutOfRangeException();

            return new LOCATORHASH(Convert.ToInt64(val) << (8 * (7 - i)));
        }

        public static LOCATORHASH InitBytes(byte[] val, int startIndex)
        {
            if (val.GetLength(0) > 8) throw new IndexOutOfRangeException();

            return new LOCATORHASH(BitConverter.ToInt64(val, startIndex));
        }

        public static LOCATORHASH InitInt(int i, int val)
        {
            if (i > 1) throw new IndexOutOfRangeException();

            return new LOCATORHASH(Convert.ToInt64(val) << (32 * (1 - i)));
        }

        public static LOCATORHASH InitLong(int i, long val)
        {
            if (i > 0) throw new IndexOutOfRangeException();

            return new LOCATORHASH(val);
        }

        public static LOCATORHASH InitZero()
        {
            return InitLong(0, 0);
        }

        public LOCATORCOMP LocatorComp(LOCATORHASH other)
        {
            return new LOCATORCOMP(this, other);
        }

        public bool HasPrefix(LOCATORHASH fullPrefix, short prefixBits)
        {
            return (fullPrefix == this.GetPrefix(prefixBits));
        }

        public bool HasSuffix(LOCATORHASH fullSuffix, short prefixBits)
        {
            return (fullSuffix == this.GetPrefix(prefixBits));
        }

        public override string ToString() => "0x" + Id0.ToString("X16");
    };
    
    struct PROTOCOL_ID
    {
    };
    struct SESSION_ID
    {
    };
    struct SERVICE_INSTANCE_ID
    {
    };

    enum INPUT_SESSION_OPTION
    {
        InputSessionOption_Raw = 0,
        InputSessionOption_Drop,
        InputSessionOption_Reset,
    };

    // This is a simulated implementation of the upper edge of EccFlow.
    class EccFlow
    {
        public EccFlow (EccFlowConstraints eccFlowConstraints, LOCATORHASH locatorHash)
        {
        }

        void CloseEccFlow()
        {
        }

        // Listen for new incoming sessions from any EccFlow
        IsoService ListenAll(PROTOCOL_ID protocolId, INPUT_SESSION_OPTION inputSessionOption)
        {
            throw new NotImplementedException();
        }

        // Listen for new incoming sessions from a specific EccFlow
        IsoService ListenFlow(PROTOCOL_ID protocolId, INPUT_SESSION_OPTION inputSessionOption)
        {
            throw new NotImplementedException();
        }

        // Prepare for a specific incoming SessionId
        IsoSessionIn NewSession(SESSION_ID sessionId, INPUT_SESSION_OPTION inputSessionOption)
        {
            throw new NotImplementedException();
        }

        // New outgoing session(with optional matching input session)
        IsoSessionOut NewSession(PROTOCOL_ID protocolId, IsoSessionIn sessionIn, SERVICE_INSTANCE_ID serviceInstanceId)
        {
            throw new NotImplementedException();
        }
    }

    class IsoService
    {
        // Block waiting to Accept an incoming SessionId
        IsoSessionIn AcceptSession()
        {
            throw new NotImplementedException();
        }

        // Poll to Accept an incoming SessionId
        IsoSessionIn TryAcceptSession()
        {
            throw new NotImplementedException();
        }

    }

    class IsoSession
    {
        int send(byte[] pData, ulong size)
        {
            throw new NotImplementedException();
        }

        int recv(byte[] pData, ulong size)
        {
            throw new NotImplementedException();
        }

        void close()
        {
            throw new NotImplementedException();
        }
    }
    
    class IsoSessionIn
    {
        IsoSessionOut GetRelatedSession()
        {
            throw new NotImplementedException();
        }
    }

    class IsoSessionOut
    {
        IsoSessionIn GetRelatedSession()
        {
            throw new NotImplementedException();
        }
    }

}
