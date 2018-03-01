set XCC_DEVICE_PATH=C:\Program Files (x86)\XMOS\xTIMEcomposer\Community_14.3.2\configs
#set path=%path%C:\Program Files (x86)\XMOS\xTIMEcomposer\Community_14.3.2\lib;

"C:\Program Files (x86)\XMOS\xTIMEcomposer\Community_14.3.2\bin\xsim.exe" --plugin EthPlugin1.dll "s\\.\pipe\LoopbackTest NoTimePipe" --plugin EthPlugin2.dll "c\\.\pipe\LoopbackTest NoTimePipe" --plugin EthPlugin0.dll "c\\.\pipe\IsoSwitch00A \\.\pipe\TimePipe" ..\XMOS\IsoSwitch\bin\IsoSwitch.xe
