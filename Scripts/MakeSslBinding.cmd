
set IIS6RESKIT="%SystemDrive%\program files\iis resources"
:set SUBJECT=%COMPUTERNAME%
set SUBJECT=%1%
set VALIDDAYS=730
set KEYBITS=2048
set CERFILE=server.crt
set CERFILEBIN=server.crt.bin
set WEBROOT=%SystemDrive%\inetpub\wwwroot

:
: Create the certificate
:

%IIS6RESKIT%\SelfSSL\SelfSSL.exe /N:CN=%SUBJECT% /K:%KEYBITS% /V:%VALIDDAYS% /T /Q


: makecert.exe -r -n CN=%SUBJECT% -m %VALIDMONTHS% -len %KEYBITS% -a sha256 -eku 1.3.6.1.5.5.7.3.1 -ss MY -sr LocalMachine -sky exchange -sp "Microsoft RSA SChannel Cryptographic Provider" -sy 12

:
: Export the public part
:

certutil.exe -store MY %SUBJECT% %CERFILEBIN%


:
: Copy the certificate file to where a client can download it
:

certutil -encode %CERFILEBIN% %CERFILE%

copy /y %CERFILE% %WEBROOT%\%CERFILE%

: certutil.exe -addstore ROOT %CERFILE%

:
: Clean up
:

del %CERFILEBIN%
del %CERFILE%