// This file is not sufficiently creative to be copyrightable

#ifndef _SpiSocPlugin_H_
#define _SpiSocPlugin_H_

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

#endif /* _SpiSocPlugin_H_ */
