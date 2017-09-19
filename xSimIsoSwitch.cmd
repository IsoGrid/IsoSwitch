set XCC_DEVICE_PATH=C:\Program Files (x86)\XMOS\xTIMEcomposer\Community_14.2.4\configs
#set path=%path%C:\Program Files (x86)\XMOS\xTIMEcomposer\Community_14.2.4\lib;

"C:\Program Files (x86)\XMOS\xTIMEcomposer\Community_14.2.4\bin\xsim.exe" --plugin EthPlugin1.dll "%1 \\%2\pipe\TimePipe" --plugin SpiSocPlugin.dll "\\.\pipe\IsoSwitch%3" ..\XMOS\IsoSwitch\bin\IsoSwitch.xe
