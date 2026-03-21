#Requires AutoHotkey v2.0

#.::
{
    DllCall("AllowSetForegroundWindow", "Int", -1) 
    Run("emoji picker wpf.exe")
}
