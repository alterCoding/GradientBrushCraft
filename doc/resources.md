# Resources and remarks

## MSDN

General info for netFX detection \
https://learn.microsoft.com/en-us/dotnet/framework/install/how-to-determine-which-versions-are-installed#net-framework-45-and-later-versions

About registry entries \
https://learn.microsoft.com/en-us/troubleshoot/developer/dotnet/framework/installation/determine-dotnet-versions-service-pack-levels

About inconsistencies about product/fileversion topics, breaking changes and so on \
https://learn.microsoft.com/en-us/dotnet/core/compatibility/3.0#apis-that-report-version-now-report-product-and-not-file-version

About which version on which Windoze \
https://learn.microsoft.com/en-us/dotnet/core/compatibility/3.0#apis-that-report-version-now-report-product-and-not-file-version

On neolithic researches ... about client/full profile distinction \
https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/cc656912(v=vs.100)  
:point_right: Till net40, we used to target e.g the 3.5 runtime-only ... instead of the full sdk. But I notice that targeting *NET35* from a SDK-style project builds inevitably for the *full-profile*. The "new" compilation results don't want to execute on old platforms with *client-profile*. As a result, I think we can forget about those client-profiles. 

## Tips

Dump the registry keys of interest in cmdline
```
reg query "HKLM\SOFTWARE\Microsoft\Net Framework Setup\NDP" /s
```
The following powershell snippet is by far the best concise and powerfull alternative 
```
Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP' -Recurse | Get-ItemProperty -Name version -EA 0 | Where { $_.PSChildName -Match '^(?!S)\p{L}'} | Select PSChildName, version
```
Drawback is that it mixes different information domains at the same level.

### Interesting dev-threads

About detections:
- https://stackoverflow.com/questions/49750769/how-can-i-find-the-version-of-net-run-time-programmatically
- https://stackoverflow.com/questions/1826688/get-current-net-clr-version-at-runtime

About assembly versioning \
https://stackoverflow.com/questions/64602/what-are-differences-between-assemblyversion-assemblyfileversion-and-assemblyin

## Strong and valuable source code 

https://github.com/getsentry/sentry-dotnet/blob/main/src/Sentry/PlatformAbstraction