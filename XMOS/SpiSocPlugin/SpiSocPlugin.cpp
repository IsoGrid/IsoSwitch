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

//
// Plugin to connect a simulated XMOS IsoSwitch instance to a SoC via SPI.
//
// NOTE: This is likely to be deleted as the future way to connect to an XMOS IsoSwitch
//       will be Ethernet
//

#include <stdio.h>
#include <stdlib.h>
#include <ctype.h>
#include <string.h>
#include <Windows.h>

#include "SpiSocPlugin.h"

#define CHECK_STATUS() if (status != XSI_STATUS_OK) __debugbreak()

#define VERIFY(EVAL, ...) \
if (!(EVAL)) \
{ \
  printf("SPI FAILED: " #EVAL " && " __VA_ARGS__); \
  __debugbreak(); \
}

#define CHECK_STATE(STATE) \
if (g_state != STATE) \
{ \
  printf("SPI BAD_STATE: g_state = %d instead of " #STATE, g_state); \
  __debugbreak(); \
}

#define SET_STATE(STATE) g_state = STATE;


typedef unsigned int UINT32;
#define SUBFRAME_UINT32_COUNT ((4 * 32) + 4)

enum STATE
{
  STATE_INITIAL,
  STATE_INACTIVE,
  STATE_ACTIVE,
  STATE_WAITING_FOR_DESELECT,
};

enum CLOCK_STATE
{
  CLOCK_LOW,
  CLOCK_HIGH,
};

enum SS_STATE
{
  SS_IDLE,
  SS_ACTIVE,
};

// Globals
XsiCallbacks* g_xsi;
STATE       g_state      = STATE_INITIAL;
SS_STATE    g_lastSelect = SS_ACTIVE;
CLOCK_STATE g_lastClock  = CLOCK_LOW;
UINT32 g_countUint32 = 0;
UINT32 g_countBit = 0;

UINT32 g_mosi[SUBFRAME_UINT32_COUNT] = {};
UINT32 g_miso[SUBFRAME_UINT32_COUNT] = {};

HANDLE g_hPipe = INVALID_HANDLE_VALUE;

XsiStatus (*g_pfnClock)(void*);

static void print_usage();
static XsiStatus split_args(const char* args, char* argv[]);


#define p_sclk "X0D00" // out buffered port : 32 on tile[0] : XS1_PORT_1A;
#define p_mosi "X0D11" // out buffered port : 32 on tile[0] : XS1_PORT_1D;
#define p_miso "X0D12" // in  buffered port : 32 on tile[0] : XS1_PORT_1E;
#define p_ss   "X0D23" // out          port      on tile[0] : XS1_PORT_1H;

XsiStatus ActiveClock(void* instance);
XsiStatus InitialClock(void* instance);

XsiStatus InitialClock(void* instance)
{
  unsigned int ssPinHigh;
  XsiStatus status = g_xsi->sample_pin("0", p_ss, &ssPinHigh);
  CHECK_STATUS();

  const bool ssActive = ssPinHigh ? false : true; // SS is Active-LOW

  if (g_lastSelect == SS_ACTIVE)
  {
    if (!ssActive)
    {
      // DESELECT
      printf("    DESELECT!\n");
      g_lastSelect = SS_IDLE;
      if (g_state != STATE_INITIAL)
      {
        g_pfnClock = &ActiveClock;
      }
    }
  }
  else // SS_IDLE
  {
    if (ssActive)
    {
      printf("    SELECT!\n");
      // SELECT
      g_lastSelect = SS_ACTIVE;
      SET_STATE(STATE_INACTIVE);
    }
  }

  return status;
}

/*
 * Create
 */
XsiStatus plugin_create(void** instance, XsiCallbacks* xsi, const char* arguments)
{
  printf("Hello SPI!\n");

  xsi->set_mhz(100);

  char* argv[1];
  XsiStatus status = split_args(arguments, argv);
  if (status != XSI_STATUS_OK)
  {
    print_usage();
    return status;
  }

  printf("SpiSocPlugin Connecting to Pipe: %s\n", argv[0]);

  while (g_hPipe == INVALID_HANDLE_VALUE)
  {
    Sleep(500);
    g_hPipe = CreateFile(argv[0], GENERIC_READ | GENERIC_WRITE, 0, nullptr, OPEN_EXISTING, 0, nullptr);
    printf("SpiPlugin: Connecting... GetLastError: %d\n", GetLastError());
  }

  DWORD mode = PIPE_READMODE_MESSAGE;
  SetNamedPipeHandleState(g_hPipe, &mode, NULL, NULL);
  g_xsi = xsi;

  g_pfnClock = &InitialClock;
  return XSI_STATUS_OK;
}

XsiStatus ActiveClock(void* instance)
{
  unsigned int ssPinHigh;
  XsiStatus status = g_xsi->sample_pin("0", p_ss, &ssPinHigh);
  CHECK_STATUS();

  const bool ssActive = ssPinHigh ? false : true; // SS is Active-LOW

  if (g_lastSelect == SS_ACTIVE)
  {
    if (!ssActive)
    {
      // DESELECT
      printf("    DESELECT\n");
      g_lastSelect = SS_IDLE;
      CHECK_STATE(STATE_WAITING_FOR_DESELECT);
      SET_STATE(STATE_INACTIVE);
    }
  }
  else // SS_IDLE
  {
    if (ssActive)
    {
      printf("    SELECT\n");
      // SELECT
      g_lastSelect = SS_ACTIVE;
      CHECK_STATE(STATE_INACTIVE);
      SET_STATE(STATE_ACTIVE);
    }
  }

  if (g_state != STATE_ACTIVE)
  {
    if (g_state == STATE_WAITING_FOR_DESELECT)
    {
      UINT32 clockPinHigh;
      status = g_xsi->sample_pin("0", p_sclk, &clockPinHigh);
      CHECK_STATUS();
      VERIFY(!clockPinHigh, "while STATE_WAITING_FOR_DESELECT");
    }

    return status;
  }

  UINT32 clockPinHigh;
  status = g_xsi->sample_pin("0", p_sclk, &clockPinHigh);
  CHECK_STATUS();

  if (g_lastClock == CLOCK_HIGH)
  {
    if (!clockPinHigh)
    {
      // CLOCK_FALLING: sample from mosi pin and write to g_mosi
      g_lastClock = CLOCK_LOW;

      UINT32 mosi;
      status = g_xsi->sample_pin("0", p_mosi, &mosi);
      CHECK_STATUS();

      g_mosi[g_countUint32] >>= 1;
      g_miso[g_countUint32] >>= 1;

      g_mosi[g_countUint32] |= (mosi << 31);

      g_countBit = (g_countBit + 1) % 32;
      if (g_countBit == 0)
      {
        g_countUint32++;
        if (g_countUint32 == SUBFRAME_UINT32_COUNT)
        {
          g_countUint32 = 0;
          SET_STATE(STATE_WAITING_FOR_DESELECT);

          // Write g_mosi to the named pipe
          DWORD cbWritten;
          VERIFY(WriteFile(g_hPipe, g_mosi, sizeof(g_mosi), &cbWritten, NULL), "");
          VERIFY(cbWritten == sizeof(g_mosi), "");

          // Read the next g_miso from the named pipe
          DWORD cbRead;
          BOOL bSuccess = ReadFile(g_hPipe, g_miso, sizeof(g_miso), &cbRead, NULL);
          VERIFY(bSuccess, "ReadFile FAILED: GetLastError(): %d", GetLastError());
          VERIFY(cbRead == sizeof(g_miso), "");

#ifdef BREAK_ON_NON_ZERO_DATA
          for (int x = 0; x < SUBFRAME_UINT32_COUNT; x += 4)
          {
            if (g_mosi[x] != 0)
              __debugbreak();

            if (g_miso[x] != 0)
              __debugbreak();
          }
#endif
        }
      }
    }
  }
  else // CLOCK_LOW
  {
    if (clockPinHigh)
    {
      // CLOCK_RISING: Write from g_miso to p_miso pin
      g_lastClock = CLOCK_HIGH;
      status = g_xsi->drive_pin("0", p_miso, g_miso[g_countUint32] & 0x1);
      CHECK_STATUS();
    }
  }

  return status;
}

XsiStatus plugin_clock(void* instance)
{
  return g_pfnClock(instance);
}

XsiStatus plugin_notify(void* instance, int type, unsigned arg1, unsigned arg2)
{
  return XSI_STATUS_OK;
}

XsiStatus plugin_terminate(void* instance)
{
  return XSI_STATUS_OK;
}

/*
 * Usage
 */
static void print_usage()
{
  fprintf(stderr, "Usage:\n");
  fprintf(stderr, "  SpiSocPlugin.dll <pipename>\n");
}

/*
 * Split args
 */
static XsiStatus split_args(const char *args, char *argv[])
{
  char buf[1024];

  int arg_num = 0;
  while (arg_num < 1) {
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
  
  if (arg_num != 1 || *args != '\0')
    return XSI_STATUS_INVALID_ARGS;
  else
    return XSI_STATUS_OK;
}
