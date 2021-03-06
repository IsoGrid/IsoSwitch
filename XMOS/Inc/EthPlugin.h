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

//
// Plugin to connect two simulated XMOS IsoSwitch instances via Ethernet PHY.
//
#ifndef _EthPlugin_H_
#define _EthPlugin_H_


#include <stdio.h>
#include <stdlib.h>
#include <ctype.h>
#include <string.h>
#include <Windows.h>

#include "..\system\xsiplugin.h"

#ifdef __cplusplus
extern "C" {
#endif

  DLL_EXPORT XsiStatus plugin_create(void **instance, XsiCallbacks *xsi, const char *arguments);
  DLL_EXPORT XsiStatus plugin_clock(void *instance);
  DLL_EXPORT XsiStatus plugin_notify(void *instance, int type, unsigned arg1, unsigned arg2);
  DLL_EXPORT XsiStatus plugin_terminate(void *instance);

#ifdef __cplusplus
}
#endif


/*
*
Star(0) and (2) slot port mappings
port p_eth_rxclk  = on tile[0 1]: XS1_PORT_1B; - X.D01
port p_eth_rxd    = on tile[0 1]: XS1_PORT_4A; - X.D02 - X.D03 - X.D08 - X.D09
port p_eth_rxdv   = on tile[0 1]: XS1_PORT_1C; - X.D10
port p_eth_txd    = on tile[0 1]: XS1_PORT_4B; - X.D04 - X.D05 - X.D06 - X.D07
port p_eth_txen   = on tile[0 1]: XS1_PORT_1F; - X.D13
port p_eth_txclk  = on tile[0 1]: XS1_PORT_1G; - X.D22
port p_eth_rxerr  = on tile[0 1]: XS1_PORT_4D; (bit 0) - X.D16

Triangle(1) and (3) slot port mappings
port p_eth_rxclk  = on tile[1]: XS1_PORT_1J; - X1D25
port p_eth_rxd    = on tile[1]: XS1_PORT_4E; - X1D26 - X1D27 - X1D32 - X1D33
port p_eth_rxdv   = on tile[1]: XS1_PORT_1K; - X1D34
port p_eth_txd    = on tile[1]: XS1_PORT_4F; - X1D28 - X1D29 - X1D30 - X1D31
port p_eth_txen   = on tile[1]: XS1_PORT_1L; - X1D35
port p_eth_txclk  = on tile[1]: XS1_PORT_1I; - X1D24
port p_eth_rxerr  = on tile[1]: XS1_PORT_1P; - X1D39
*/

#ifdef PORT3
#define rxclk "X0D25"
#define rxd0  "X0D26"
#define rxd1  "X0D27"
#define rxd2  "X0D32"
#define rxd3  "X0D33"
#define rxdv  "X0D34"
#define txd0  "X0D28"
#define txd1  "X0D29"
#define txd2  "X0D30"
#define txd3  "X0D31"
#define txen  "X0D35"
#define txclk "X0D24"
#define txerr "X0D39"
#define PORT 3
#define DBGPRINT(...)
#endif

#ifdef PORT2
#define rxclk "X0D01"
#define rxd0  "X0D02"
#define rxd1  "X0D03"
#define rxd2  "X0D08"
#define rxd3  "X0D09"
#define rxdv  "X0D10"
#define txd0  "X0D04"
#define txd1  "X0D05"
#define txd2  "X0D06"
#define txd3  "X0D07"
#define txen  "X0D13"
#define txclk "X0D22"
#define txerr "X0D16"
#define PORT 2
#define DBGPRINT(...)
#endif

#ifdef PORT1
#define rxclk "X1D25"
#define rxd0  "X1D26"
#define rxd1  "X1D27"
#define rxd2  "X1D32"
#define rxd3  "X1D33"
#define rxdv  "X1D34"
#define txd0  "X1D28"
#define txd1  "X1D29"
#define txd2  "X1D30"
#define txd3  "X1D31"
#define txen  "X1D35"
#define txclk "X1D24"
#define txerr "X1D39"
#define PORT 1
#define DBGPRINT(...)
#endif

#ifdef PORT0
#define rxclk "X1D01"
#define rxd0  "X1D02"
#define rxd1  "X1D03"
#define rxd2  "X1D08"
#define rxd3  "X1D09"
#define rxdv  "X1D10"
#define txd0  "X1D04"
#define txd1  "X1D05"
#define txd2  "X1D06"
#define txd3  "X1D07"
#define txen  "X1D13"
#define txclk "X1D22"
#define txerr "X1D16"
#define PORT 0
#define DBGPRINT(...) printf(__VA_ARGS__)
#endif


#define CHECK_STATUS() if (status != XSI_STATUS_OK) __debugbreak()

#define VERIFY(EVAL, MSG) \
if (!(EVAL)) \
{ \
  printf("ETH FAILED: " #EVAL " && " MSG "\n"); \
  __debugbreak(); \
}

#define CHECK_STATE(STATE) \
if (g_state != STATE) \
{ \
  printf("ETH BAD_STATE: g_state = %d instead of " #STATE "\n", g_state); \
  __debugbreak(); \
}

#define SET_STATE(STATE) g_state = STATE;


typedef unsigned int UINT32;
#define SUBFRAME_UINT32_COUNT    ((4 * 32) + 3)
#define NUM_SUBFRAMES            20
#define DEBUG_SPEED_DIVISOR      1
#define SIM_MIN_TICKS_PER_KCLOCK 650

enum TXSTATE
{
  TXSTATE_INITIAL,
  TXSTATE_INITIAL2,
  TXSTATE_DISABLED,
  TXSTATE_ENABLED,
  TXSTATE_WAIT_FOR_DISABLE,
  TXSTATE_GAP,
};

enum RXSTATE
{
  RXSTATE_GAP,
  RXSTATE_INACTIVE_GAP,
  RXSTATE_INACTIVE,
  RXSTATE_BUFFERING,
  RXSTATE_PREAMBLE,
  RXSTATE_ACTIVE,
};

// Globals
XsiCallbacks* g_xsi;
RXSTATE       g_rxState = RXSTATE_INACTIVE;
TXSTATE       g_txState = TXSTATE_INITIAL;

UINT32   g_clock = 0;
UINT32   g_lastkClockTicks;

UINT32     g_rxBuf[NUM_SUBFRAMES][SUBFRAME_UINT32_COUNT] = {};
UINT32*    g_rxCurBuf = g_rxBuf[0];
OVERLAPPED g_rxOv = {};
UINT32     g_rxBit = 0;
UINT32     g_rxUint32 = 0;
UINT32     g_rxSubframe = 0;
UINT32     g_rxFrame = 0;
UINT32     g_rxLastClock = 0;
UINT32     g_rxPreamble = 0;

UINT32     g_txBuf[NUM_SUBFRAMES][SUBFRAME_UINT32_COUNT] = {};
UINT32*    g_txCurBuf = g_txBuf[0];
OVERLAPPED g_txOv = {};
UINT32     g_txPreambleUint32 = 0;
UINT32     g_txBit = 0;
UINT32     g_txUint32 = 0;
UINT32     g_txSubframe = 0;
UINT32     g_txFrame = 0;
UINT32     g_txLastClock = 0;

HANDLE     g_hPipe = INVALID_HANDLE_VALUE;
bool       g_isConnected = false;
bool       g_isDoubleInit = false;

HANDLE     g_hTimePipe = INVALID_HANDLE_VALUE;
bool       g_isServer = false;

static void print_usage();
static XsiStatus split_args(const char* args, char* argv[]);

__inline void Init()
{
  if (!g_isConnected && g_hPipe != INVALID_HANDLE_VALUE)
  {
    printf("EthPlugin: Waiting for connection...\n");
    WaitForSingleObject(g_rxOv.hEvent, INFINITE);
    g_isConnected = true;

    ResetEvent(g_rxOv.hEvent);
    ReadFile(g_hPipe, g_rxCurBuf, SUBFRAME_UINT32_COUNT * sizeof(UINT32), nullptr, &g_rxOv);
    VERIFY(GetLastError() == ERROR_IO_PENDING, "i");

    SetEvent(g_txOv.hEvent);
    g_txOv.InternalHigh = SUBFRAME_UINT32_COUNT * sizeof(UINT32);

    g_lastkClockTicks = GetTickCount();

    if (g_isDoubleInit)
    {
      printf("EthPlugin: Waiting a bit to slow things down for double-init (debug) case");
      Sleep(13000);
    }
  }
}

/*
* Create
*/
XsiStatus plugin_create(void** instance, XsiCallbacks* xsi, const char* arguments)
{
  printf("hello ETH! ");

  xsi->set_mhz(100.0 / DEBUG_SPEED_DIVISOR);

  char* argv[3];
  XsiStatus status = split_args(arguments, argv);
  if (status != XSI_STATUS_OK)
  {
    print_usage();
    return status;
  }

  g_txOv.hEvent = CreateEvent(nullptr, true, false, nullptr);
  g_rxOv.hEvent = CreateEvent(nullptr, true, false, nullptr);

  PCSTR szPipeName = argv[0] + 1;
  PCSTR szTimePipeName = argv[1];

  if (szTimePipeName[0] == '\\')
  {
    printf("EthPlugin Client Connecting to TimePipe: %s\n", szTimePipeName);

    while (g_hTimePipe == INVALID_HANDLE_VALUE)
    {
      Sleep(500);
      g_hTimePipe = CreateFile(szTimePipeName, GENERIC_READ | GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, 0, nullptr);
      DWORD dwErr = GetLastError();
      printf("EthPlugin: ConnectingTimePipe... GetLastError: %d\n", dwErr);
    }

    DWORD mode = PIPE_READMODE_MESSAGE;
    SetNamedPipeHandleState(g_hTimePipe, &mode, nullptr, nullptr);
  }
  else
  {
    printf("EthPlugin: No Forced TimePipe synchronization.");
  }

  if (argv[0][0] == 'c') // Client
  {
    printf("EthPlugin Client Connecting to Pipe: %s\n", szPipeName);

    while (g_hPipe == INVALID_HANDLE_VALUE)
    {
      Sleep(500);
      g_hPipe = CreateFile(szPipeName, GENERIC_READ | GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, FILE_FLAG_OVERLAPPED, nullptr);
      DWORD dwErr = GetLastError();
      printf("EthPlugin: Connecting... GetLastError: %d\n", dwErr);
    }

    DWORD mode = PIPE_READMODE_MESSAGE;
    SetNamedPipeHandleState(g_hPipe, &mode, nullptr, nullptr);

    printf("EthPlugin: Connected!\n");
    SetEvent(g_rxOv.hEvent); // CreateFile call was synchronous, so mark it complete for Init()
    Init();
  }
  else if (argv[0][0] == 's' || argv[0][0] == 'd')
  {
    g_isServer = true;

    printf("EthPlugin Server Starting Pipe: %s\n", szPipeName);

    g_hPipe = CreateNamedPipe(szPipeName,
      PIPE_ACCESS_DUPLEX | FILE_FLAG_FIRST_PIPE_INSTANCE | FILE_FLAG_OVERLAPPED,
      PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_ACCEPT_REMOTE_CLIENTS,
      2,
      NUM_SUBFRAMES * SUBFRAME_UINT32_COUNT * sizeof(UINT32),
      NUM_SUBFRAMES * SUBFRAME_UINT32_COUNT * sizeof(UINT32),
      0,
      nullptr);

    if (argv[0][0] == 'd')
    {
      ConnectNamedPipe(g_hPipe, &g_rxOv);

      WaitForSingleObject(g_rxOv.hEvent, INFINITE);
      DisconnectNamedPipe(g_hPipe);

      ResetEvent(g_rxOv.hEvent);

      g_isDoubleInit = true;
    }

    printf("EthPlugin: ConnectNamedPipe...\n");
    if (ConnectNamedPipe(g_hPipe, &g_rxOv))
    {
      Init();
    }
    else if (GetLastError() == ERROR_PIPE_CONNECTED)
    {
      SetEvent(g_rxOv.hEvent);
      Init();
    }
    else if (GetLastError() != ERROR_IO_PENDING)
    {
      printf("EthPlugin: ConnectNamedPipe() GetLastError: %d\n", GetLastError());
      VERIFY(FALSE, "ConnectNamedPipe FAILED!");
    }

    if (!g_isDoubleInit)
    {
      printf("EthPlugin: Slowing down to make sure it starts at the same time as others.\n");
      Sleep(800);
    }
  }
  else
  {
    printf("EthPlugin: Unknown argument0 %s\n", argv[0]);
    print_usage();
    return status;
  }

  g_xsi = xsi;

  return XSI_STATUS_OK;
}

XsiStatus plugin_clock(void* instance)
{
  Init();

  XsiStatus status;

  g_clock++;

  /*
   * This could be used to implement SIM_MIN_TICKS_PER_KCLOCK
  if ((g_clock % 1024 == 0) && (g_hTimePipe == INVALID_HANDLE_VALUE))
  {
    g_lastkClockTicks += SIM_MIN_TICKS_PER_KCLOCK;

    UINT32 curTickCount = GetTickCount();

    VERIFY(curTickCount < g_lastkClockTicks, "Slow processor, need larger SIM_MIN_TICKS_PER_KCLOCK");

    while (curTickCount < g_lastkClockTicks)
    {
      Sleep(1);
      curTickCount = GetTickCount();
    }
  }
  */

  UINT32 txEnabledPin;

  if (g_clock % (4) == 0)
  {
    if ((g_rxState == RXSTATE_GAP) || (g_rxState == RXSTATE_INACTIVE_GAP))
    {
      g_rxBit += 4;
      if (g_rxBit == 32)
      {
        g_rxBit = 0;
        g_rxUint32++;

        // Gap is 12 octets = (3 * 32 bits)
        if (g_rxUint32 == 3)
        {
          g_rxUint32 = 0;
          g_rxState = (g_rxState == RXSTATE_INACTIVE_GAP) ? RXSTATE_INACTIVE : RXSTATE_PREAMBLE;
        }
      }
    }

    if (g_rxState == RXSTATE_INACTIVE)
    {
      if (WaitForSingleObject(g_rxOv.hEvent, 0) == WAIT_OBJECT_0)
      {
        g_rxState = RXSTATE_BUFFERING;

        VERIFY(g_rxOv.Internal == ERROR_SUCCESS, "");
        VERIFY(g_rxOv.InternalHigh == SUBFRAME_UINT32_COUNT * sizeof(UINT32), "");
        DBGPRINT("    ETH First Complete %d\n", g_rxSubframe);
        g_rxLastClock = g_clock;

        // Start a new read on the next buffer
        ResetEvent(g_rxOv.hEvent);
        ReadFile(g_hPipe, g_rxBuf[1], SUBFRAME_UINT32_COUNT * sizeof(UINT32), nullptr, &g_rxOv);
        VERIFY(GetLastError() == ERROR_IO_PENDING, "1");
      }
    }

    if (g_rxState == RXSTATE_BUFFERING)
    {
      if (WaitForSingleObject(g_rxOv.hEvent, 0) == WAIT_OBJECT_0)
      {
        DBGPRINT("    ETH Read: %d\n", g_clock - g_rxLastClock);
        g_rxLastClock = g_clock;

        g_rxState = RXSTATE_PREAMBLE;
      }
    }
    
    if (g_rxState == RXSTATE_PREAMBLE)
    {
      if (g_rxBit == 0)
      {
        g_xsi->drive_pin("0", rxdv, 1);

        switch (g_rxUint32)
        {
        case 0: g_rxPreamble = 0x55555555; break; // Beginning of Preamble
        case 1: g_rxPreamble = 0xFFFFD555; break; // End Preamble - Start Frame Delimiter - Beginning of Dest MAC address field
        case 2: g_rxPreamble = 0xFFFFFFFF; break; // End of Dest MAC address field
        case 3: g_rxPreamble = 0x00000000; break; // Beginning of Source MAC address field
        case 4: g_rxPreamble = 0x00650000; break; // End of Source MAC address field - 0x6500 IsoSwitch EtherType
        }
      }

      g_xsi->drive_pin("0", rxd0, g_rxPreamble & 0x1);
      g_rxPreamble >>= 1;
      g_xsi->drive_pin("0", rxd1, g_rxPreamble & 0x1);
      g_rxPreamble >>= 1;
      g_xsi->drive_pin("0", rxd2, g_rxPreamble & 0x1);
      g_rxPreamble >>= 1;
      g_xsi->drive_pin("0", rxd3, g_rxPreamble & 0x1);
      g_rxPreamble >>= 1;

      g_rxBit += 4;
      if (g_rxBit == 32)
      {
        g_rxBit = 0;
        g_rxUint32++;

        if (g_rxUint32 == 5)
        {
          g_rxUint32 = 0;
          g_rxState = RXSTATE_ACTIVE;
        }
      }
    }
    else if (g_rxState == RXSTATE_ACTIVE)
    {
      if (g_rxBit == 0)
      {
        VERIFY(g_rxCurBuf[g_rxUint32] != 0xBADDF00D, "XMOS Unintialized Memory!");

        if (g_rxUint32 == 0)
        {
#ifdef PRINT_RECEIVED
          // Note, the CRC for an empty frame is 0x7647C33C

          printf("Port: %d, Subframe: %d\n", PORT, g_rxSubframe);
          for (int i = 0; i < SUBFRAME_UINT32_COUNT; i++)
          {
            printf("%X ", g_rxCurBuf[i]);
          }
          printf("\n");
#endif
        }
      }

      g_xsi->drive_pin("0", rxd0, g_rxCurBuf[g_rxUint32] & 0x1);
      g_rxCurBuf[g_rxUint32] >>= 1;
      g_xsi->drive_pin("0", rxd1, g_rxCurBuf[g_rxUint32] & 0x1);
      g_rxCurBuf[g_rxUint32] >>= 1;
      g_xsi->drive_pin("0", rxd2, g_rxCurBuf[g_rxUint32] & 0x1);
      g_rxCurBuf[g_rxUint32] >>= 1;
      g_xsi->drive_pin("0", rxd3, g_rxCurBuf[g_rxUint32] & 0x1);
      g_rxCurBuf[g_rxUint32] >>= 1;

      g_rxBit += 4;
      if (g_rxBit == 32)
      {
        g_rxBit = 0;
        g_rxUint32++;
        
        if (g_rxUint32 == SUBFRAME_UINT32_COUNT)
        {
          g_rxUint32 = 0;
          g_rxSubframe++;

          // Increment rxBuf
          g_rxCurBuf = g_rxBuf[g_rxSubframe];

          g_rxState = RXSTATE_GAP;
          
          if (g_rxSubframe == NUM_SUBFRAMES)
          {
            g_rxSubframe = 0;
            g_rxCurBuf = g_rxBuf[0];
            g_rxFrame++;

            g_rxLastClock = g_clock;

            // Queue up a read on the next frame
            ResetEvent(g_rxOv.hEvent);
            ReadFile(g_hPipe, g_rxCurBuf, SUBFRAME_UINT32_COUNT * sizeof(UINT32), nullptr, &g_rxOv);
            VERIFY(GetLastError() == ERROR_IO_PENDING, "0");

            DBGPRINT("    ETH Queue Next Frame Read\n");
            g_rxState = RXSTATE_INACTIVE_GAP;
          }
          else if (g_rxSubframe == 1)
          {
            DBGPRINT("    ETH Read: %d  1\n", g_clock - g_rxLastClock);
            g_rxLastClock = g_clock;
          }
          else
          {
            DBGPRINT("    ETH Read: %d ", g_clock - g_rxLastClock);
            g_rxLastClock = g_clock;


            // Start a new read on the next buffer
            ResetEvent(g_rxOv.hEvent);
            ReadFile(g_hPipe, g_rxBuf[g_rxSubframe], SUBFRAME_UINT32_COUNT * sizeof(UINT32), nullptr, &g_rxOv);
            VERIFY(GetLastError() == ERROR_IO_PENDING, "n");

            // If this hangs, it likely means that the RX is being driven faster than the pipe is 
            // able to deliver subframes.
            WaitForSingleObject(g_rxOv.hEvent, INFINITE);

            VERIFY(g_rxOv.Internal == ERROR_SUCCESS, "");
            VERIFY(g_rxOv.InternalHigh == SUBFRAME_UINT32_COUNT * sizeof(UINT32), "");
            DBGPRINT(" %d\n", g_rxSubframe);
          }
        }
      }
    }

    switch (g_txState)
    {
    case TXSTATE_INITIAL:
      g_xsi->sample_pin("0", txen, &txEnabledPin);
      if (txEnabledPin)
      {
        g_txState = TXSTATE_INITIAL2;
      }
      break;

    case TXSTATE_INITIAL2:
      g_xsi->sample_pin("0", txen, &txEnabledPin);
      if (!txEnabledPin)
      {
        g_txState = TXSTATE_DISABLED;
      }
      break;

    case TXSTATE_DISABLED:
      g_xsi->sample_pin("0", txen, &txEnabledPin);
      if (!txEnabledPin)
      {
        break;
      }
      g_txState = TXSTATE_ENABLED;
      __fallthrough;

    case TXSTATE_ENABLED:
      UINT32 pinVal;
      g_xsi->sample_pin("0", txd0, &pinVal);
      g_txCurBuf[g_txUint32] = (g_txCurBuf[g_txUint32] >> 1) | (pinVal << 31);
      g_xsi->sample_pin("0", txd1, &pinVal);
      g_txCurBuf[g_txUint32] = (g_txCurBuf[g_txUint32] >> 1) | (pinVal << 31);
      g_xsi->sample_pin("0", txd2, &pinVal);
      g_txCurBuf[g_txUint32] = (g_txCurBuf[g_txUint32] >> 1) | (pinVal << 31);
      g_xsi->sample_pin("0", txd3, &pinVal);
      g_txCurBuf[g_txUint32] = (g_txCurBuf[g_txUint32] >> 1) | (pinVal << 31);

      g_txBit += 4;
      if (g_txBit == 32)
      {
        VERIFY(g_txCurBuf[g_txUint32] != 0xBADDF00D, "XMOS Unintialized Memory!");

        g_txBit = 0;

        switch (g_txPreambleUint32++)
        {
        case 0:
          VERIFY(g_txCurBuf[0] == 0x55555555, ""); // Beginning of Preamble
          g_txCurBuf[0] = 0;
          break;
        case 1:
          VERIFY(g_txCurBuf[0] == 0xFFFFD555, ""); // End Preamble - Start Frame Delimiter - Beginning of Dest MAC address
          g_txCurBuf[0] = 0;
          break;
        case 2:
          VERIFY(g_txCurBuf[0] == 0xFFFFFFFF, ""); // End of Dest MAC address
          g_txCurBuf[0] = 0;
          break;
        case 3:
          VERIFY(g_txCurBuf[0] == 0x00000000, ""); // Beginning of Source MAC address
          g_txCurBuf[0] = 0;
          break;
        case 4:
          VERIFY(g_txCurBuf[0] == 0x00650000, ""); // End of Source MAC address - 0x6500 IsoSwitch EtherType
          g_txCurBuf[0] = 0;
          g_txUint32 = 0;
          break;

        default:
          g_txUint32++;
          break;
        }

        if (g_txUint32 == SUBFRAME_UINT32_COUNT)
        {
          g_txUint32 = 0;

          if (g_txCurBuf[SUBFRAME_UINT32_COUNT - 2] != 0)
          {
            printf("**** #%d Erased Words in Frame! 0x%X\n", PORT, g_txCurBuf[SUBFRAME_UINT32_COUNT - 2]);
          }

          DBGPRINT("    ETH Write: %d ", g_clock - g_txLastClock);
          g_txLastClock = g_clock;

          // Wait for the previous write to complete
          WaitForSingleObject(g_txOv.hEvent, INFINITE);
          VERIFY(g_txOv.Internal == ERROR_SUCCESS, "");
          VERIFY(g_txOv.InternalHigh == SUBFRAME_UINT32_COUNT * sizeof(UINT32), "");

          DBGPRINT("            %d\n", g_txSubframe);

          g_txSubframe = (g_txSubframe + 1) % NUM_SUBFRAMES;

          // Swap txBuf
          UINT32* txLastBuf = g_txCurBuf;
          g_txCurBuf = g_txBuf[g_txSubframe];

          if (g_hTimePipe != INVALID_HANDLE_VALUE)
          {
            char reply[4];
            VERIFY(WriteFile(g_hTimePipe, "Done", 4, nullptr, nullptr), "");
            VERIFY(ReadFile(g_hTimePipe, reply, 4, nullptr, nullptr), "");
          }

          // Start a new write using the most recent buffer
          ResetEvent(g_txOv.hEvent);
          VERIFY(WriteFile(g_hPipe, txLastBuf, SUBFRAME_UINT32_COUNT * sizeof(UINT32), nullptr, &g_txOv), "");

#ifdef PRINT_TRANSMITTED
          // Note, the CRC for an empty frame is 0x7647C33C\

          printf("Port: %d, Subframe: %d\n", PORT, (g_txSubframe == 0) ? 19 : (g_txSubframe - 1) % NUM_SUBFRAMES);
          for (int i = 0; i < SUBFRAME_UINT32_COUNT; i++)
          {
            printf("%X ", txLastBuf[i]);
          }
          printf("\n");
#endif

          g_txPreambleUint32 = 0;
          g_txState = TXSTATE_WAIT_FOR_DISABLE;
        }

        g_xsi->sample_pin("0", txen, &txEnabledPin);
        VERIFY(txEnabledPin, "TXSTATE_ENABLED_* RISING");
      }
      break;

    case TXSTATE_WAIT_FOR_DISABLE:
      g_xsi->sample_pin("0", txen, &txEnabledPin);
      if (!txEnabledPin) // Disabled
      {
        g_txState = TXSTATE_GAP;
        break;
      }
      break;

    case TXSTATE_GAP:
      g_xsi->sample_pin("0", txen, &txEnabledPin);
      VERIFY(!txEnabledPin, "Interframe Gap too small!");

      g_txBit += 4;
      if (g_txBit == 32)
      {
        g_txBit = 0;
        g_txUint32++;
        if (g_txUint32 == 3)
        {
          g_txUint32 = 0;
          g_txState = TXSTATE_DISABLED;
        }
      }
      break;

    default:
      break;
    }

    status = g_xsi->drive_pin("0", rxclk, 1);
    status = g_xsi->drive_pin("0", txclk, 1);
    CHECK_STATUS();
  }
  else if (g_clock % (4) == (2))
  {
    status = g_xsi->drive_pin("0", rxclk, 0);
    status = g_xsi->drive_pin("0", txclk, 0);
    CHECK_STATUS();

    if ((g_rxState == RXSTATE_GAP) || (g_rxState == RXSTATE_INACTIVE_GAP))
    {
      status = g_xsi->drive_pin("0", rxdv, 0);
      CHECK_STATUS();
    }

    switch (g_txState)
    {
    case TXSTATE_ENABLED:
      g_xsi->sample_pin("0", txen, &txEnabledPin);

      // If txen goes low in the middle of a frame, 
      // it often indicates that the TX_FUNC wasn't able to keep up
      VERIFY(txEnabledPin, "TXSTATE_ENABLED txen fall (TX_FUNC wasn't able to keep up)");
      break;

    case TXSTATE_WAIT_FOR_DISABLE:
      g_xsi->sample_pin("0", txen, &txEnabledPin);
      if (!txEnabledPin) // Disabled
      {
        g_txState = TXSTATE_GAP;
        break;
      }
      break;
    }
  }

  return XSI_STATUS_OK;
}

XsiStatus plugin_notify(void* instance, int type, unsigned arg1, unsigned arg2)
{
  printf("    EthPlugin: Notify: %d, %d, %d", type, arg1, arg2);

  return XSI_STATUS_OK;
}

XsiStatus plugin_terminate(void* instance)
{
  printf("    EthPlugin: Terminated\n");

  if (g_hPipe != INVALID_HANDLE_VALUE)
  {
    CloseHandle(g_hPipe);
    g_hPipe = INVALID_HANDLE_VALUE;
  }
  return XSI_STATUS_OK;
}

/*
* Usage
*/
static void print_usage()
{
  fprintf(stderr, "Usage:\n");
  fprintf(stderr, "  EthPlugin#.dll c<pipename> <timeserverpipe>\n");
  fprintf(stderr, "  EthPlugin#.dll s<pipename> <timeserverpipe>\n");
  fprintf(stderr, "  EthPlugin#.dll d<pipename> <timeserverpipe>\n");
  fprintf(stderr, "  c -> client connect \n");
  fprintf(stderr, "  s -> server create \n");
  fprintf(stderr, "  d -> debug server create \n");
  fprintf(stderr, "  timeserverpipe can be 'isoch' to ignore it and run full isoch \n");
  fprintf(stderr, "  # -> The ethernet port number (1-3)");
}

/*
* Split args
*/
static XsiStatus split_args(const char *args, char *argv[])
{
  char buf[1024];

  int arg_num = 0;
  while (arg_num < 2)
  {
    char *buf_ptr = buf;

    while (isspace(*args))
      args++;

    if (*args == '\0')
      return XSI_STATUS_INVALID_ARGS;

    while (*args != '\0' && !isspace(*args))
      *buf_ptr++ = *args++;

    *buf_ptr = '\0';
    argv[arg_num] = _strdup(buf);
    arg_num++;
  }

  while (isspace(*args))
    args++;

  if (arg_num != 2 || *args != '\0')
    return XSI_STATUS_INVALID_ARGS;
  else
    return XSI_STATUS_OK;
}


#endif /* _EthPlugin_H_ */

