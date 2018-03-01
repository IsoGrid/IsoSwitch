set XCC_DEVICE_PATH=C:\Program Files (x86)\XMOS\xTIMEcomposer\Community_14.3.2\configs
#set path=%path%C:\Program Files (x86)\XMOS\xTIMEcomposer\Community_14.3.2\lib;

"C:\Program Files (x86)\XMOS\xTIMEcomposer\Community_14.3.2\bin\xsim.exe" --plugin EthPlugin1.dll "%1 NoTimePipe" --plugin EthPlugin0.dll "c\\.\pipe\IsoSwitch%3 \\%2\pipe\TimePipe" ..\XMOS\IsoSwitch\bin\IsoSwitch.xe
