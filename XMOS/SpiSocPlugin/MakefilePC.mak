TOOLS_ROOT = ../../..
!INCLUDE $(TOOLS_ROOT)/src/MakefilePc.mak

OBJS = SpiSocPlugin.obj

all: $(DLLDIR)/SpiSocPlugin.dll

"$(DLLDIR)/SpiSocPlugin.dll": $(OBJS)
    $(LINK32) $(LINK32_LIBS) /DLL /nologo /out:"$(DLLDIR)/SpiSocPlugin.dll" @<<
    $(LINKFLAGS) $(OBJS)
<<

.cpp{}.obj::
    $(CPP) @<<
    $(CFLAGS) -I$(TOOLS_ROOT)/include $<
<<

clean:
    -@rm $(OBJS) *.idb *.pdb 2> NUL
    -@rm $(DLLDIR)/SpiSocPlugin.* 2> NUL
 